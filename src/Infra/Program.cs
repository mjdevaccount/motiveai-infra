using Amazon.CDK;
using Infra.Stacks;

var app = new App();

var env = new Amazon.CDK.Environment
{
    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
};

var authStack = new AuthStack(app, "MotiveAI-Auth", new StackProps { Env = env });
var apiStack = new ApiStack(app, "MotiveAI-Api", new StackProps { Env = env });
var frontendStack = new FrontendStack(app, "MotiveAI-Frontend", new StackProps { Env = env });

app.Synth();