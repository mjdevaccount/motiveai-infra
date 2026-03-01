using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;
using System.Collections.Generic;

namespace Infra.Stacks
{
    public class ApiStack : Stack
    {
        public ApiStack(Construct scope, string id, StackProps props) : base(scope, id, props)
        {
            // Claude API key — value set manually after deploy, never in code
            var claudeSecret = new Secret(this, "ClaudeApiKey", new SecretProps
            {
                SecretName = "motiveai/claude-api-key",
                Description = "Anthropic Claude API key for MotiveAI"
            });

            // API Gateway
            var api = new RestApi(this, "MotiveAIApi", new RestApiProps
            {
                RestApiName = "motiveai-api",
                DefaultCorsPreflightOptions = new CorsOptions
                {
                    AllowOrigins = Cors.ALL_ORIGINS,
                    AllowMethods = Cors.ALL_METHODS,
                    AllowHeaders = new[] { "Authorization", "Content-Type" }
                }
            });

            // Lambda function
            var analyzeFunction = new Function(this, "AnalyzeFunction", new FunctionProps
            {
                FunctionName = "motiveai-analyze",
                Runtime = Runtime.DOTNET_8,
                Handler = "MotiveAI.Lambda::MotiveAI.Lambda.Function::FunctionHandler",
                Code = Code.FromAsset(@"C:\motiveai\app\lambda\MotiveAI.Lambda\publish"),
                Timeout = Duration.Seconds(30),
                MemorySize = 512,
                Environment = new Dictionary<string, string>
                {
                    ["CLAUDE_SECRET_ARN"] = claudeSecret.SecretArn
                }
            });

            // Events Lambda — fetches latest from GDELT, no secrets needed
            var eventsFunction = new Function(this, "EventsFunction", new FunctionProps
            {
                FunctionName = "motiveai-events",
                Runtime = Runtime.DOTNET_8,
                Handler = "MotiveAI.Events::MotiveAI.Events.Function::FunctionHandler",
                Code = Code.FromAsset(@"C:\motiveai\app\lambda\MotiveAI.Events\publish"),
                Timeout = Duration.Seconds(30),
                MemorySize = 256
            });

            var analyze = api.Root.AddResource("analyze");
            analyze.AddMethod("POST", new LambdaIntegration(analyzeFunction));

            var events = api.Root.AddResource("events");
            events.AddMethod("GET", new LambdaIntegration(eventsFunction));

            new CfnOutput(this, "EventsUrl", new CfnOutputProps { Value = $"{api.Url}events" });
            new CfnOutput(this, "ApiUrl", new CfnOutputProps { Value = api.Url });

            // Single-table for actors, claims, and analyses
            var dataTable = new Table(this, "DataTable", new TableProps
            {
                TableName = "motiveai-data",
                PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "SK", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.RETAIN
            });

            // GSI1 — public feed sorted by recency
            // Items populate GSI1PK = "FEED", GSI1SK = <timestamp>
            dataTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "GSI1",
                PartitionKey = new Attribute { Name = "GSI1PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "GSI1SK", Type = AttributeType.STRING }
            });

            // GSI2 — claim maturity checks per actor
            // Items populate GSI2SK = MATURITY#<date>; reuses table PK (ACTOR#<id>)
            dataTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "GSI2",
                PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "GSI2SK", Type = AttributeType.STRING }
            });

            new CfnOutput(this, "DataTableArn", new CfnOutputProps { Value = dataTable.TableArn });

            // Agent Lambda — scheduled background worker, no API Gateway route
            var agentFunction = new Function(this, "AgentFunction", new FunctionProps
            {
                FunctionName = "motiveai-agent",
                Runtime = Runtime.DOTNET_8,
                Handler = "MotiveAI.Agent::MotiveAI.Agent.Function::FunctionHandler",
                Code = Code.FromAsset(@"C:\motiveai\app\lambda\MotiveAI.Agent\publish"),
                Timeout = Duration.Seconds(60),
                MemorySize = 512,
                Environment = new Dictionary<string, string>
                {
                    ["DATA_TABLE_NAME"] = dataTable.TableName
                }
            });

            dataTable.GrantReadWriteData(agentFunction);

            // EventBridge rule — fires every 30 minutes
            var agentSchedule = new Rule(this, "AgentScheduleRule", new RuleProps
            {
                Schedule = Schedule.Rate(Duration.Minutes(30))
            });
            agentSchedule.AddTarget(new LambdaFunction(agentFunction));
        }
    }
}