using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Constructs;

namespace Infra.Stacks
{
    public class AuthStack : Stack
    {
        public UserPool UserPool { get; }
        public UserPoolClient UserPoolClient { get; }

        public AuthStack(Construct scope, string id, StackProps props) : base(scope, id, props)
        {
            UserPool = new UserPool(this, "MotiveAIUserPool", new UserPoolProps
            {
                UserPoolName = "motiveai-users",
                SelfSignUpEnabled = false,
                SignInAliases = new SignInAliases { Email = true },
                AutoVerify = new AutoVerifiedAttrs { Email = true },
                PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 12,
                    RequireUppercase = true,
                    RequireLowercase = true,
                    RequireDigits = true,
                    RequireSymbols = true
                },
                AccountRecovery = AccountRecovery.EMAIL_ONLY,
                RemovalPolicy = RemovalPolicy.RETAIN
            });

            UserPoolClient = UserPool.AddClient("MotiveAIWebClient", new UserPoolClientOptions
            {
                UserPoolClientName = "motiveai-web",
                GenerateSecret = false,
                AuthFlows = new AuthFlow { UserSrp = true },
                OAuth = new OAuthSettings
                {
                    Flows = new OAuthFlows { AuthorizationCodeGrant = true },
                    Scopes = new[] { OAuthScope.EMAIL, OAuthScope.OPENID, OAuthScope.PROFILE },
                    CallbackUrls = new[] { "http://localhost:3000/callback" },
                    LogoutUrls = new[] { "http://localhost:3000" }
                },
                AccessTokenValidity = Duration.Hours(1),
                IdTokenValidity = Duration.Hours(1),
                RefreshTokenValidity = Duration.Days(30)
            });

            UserPool.AddDomain("MotiveAIDomain", new UserPoolDomainOptions
            {
                CognitoDomain = new CognitoDomainOptions { DomainPrefix = "motiveai-auth" }
            });

            new CfnOutput(this, "UserPoolId", new CfnOutputProps { Value = UserPool.UserPoolId });
            new CfnOutput(this, "UserPoolClientId", new CfnOutputProps { Value = UserPoolClient.UserPoolClientId });
        }
    }
}