using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
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
        }
    }
}