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

            // Lambda function
            //var analyzeFunction = new Function(this, "AnalyzeFunction", new FunctionProps
            //{
            //    FunctionName = "motiveai-analyze",
            //    Runtime = Runtime.DOTNET_8,
            //    Handler = "MotiveAI.App::MotiveAI.App.Functions.AnalyzeFunction::FunctionHandler",
            //    Code = Code.FromAsset(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "placeholder.zip"), new Amazon.CDK.AWS.Lambda.AssetOptions()),
            //    Timeout = Duration.Seconds(30),
            //    MemorySize = 512,
            //    Environment = new Dictionary<string, string>
            //    {
            //        ["CLAUDE_SECRET_ARN"] = claudeSecret.SecretArn
            //    }
            //});

            // Grant Lambda permission to read the secret
            //claudeSecret.GrantRead(analyzeFunction);

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

            //var analyze = api.Root.AddResource("analyze");
            //analyze.AddMethod("POST", new LambdaIntegration(analyzeFunction));

            new CfnOutput(this, "ApiUrl", new CfnOutputProps { Value = api.Url });
        }
    }
}