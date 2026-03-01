using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Constructs;

namespace Infra.Stacks
{
    public class FrontendStack : Stack
    {
        public FrontendStack(Construct scope, string id, StackProps props) : base(scope, id, props)
        {
            // Private S3 bucket — CloudFront is the only way in
            var siteBucket = new Bucket(this, "SiteBucket", new BucketProps
            {
                BucketName = "motiveai-frontend",
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                RemovalPolicy = RemovalPolicy.DESTROY,
                AutoDeleteObjects = true
            });

            // CloudFront — HTTPS, global CDN, free SSL
            var distribution = new Distribution(this, "SiteDistribution", new DistributionProps
            {
                DefaultBehavior = new BehaviorOptions
                {
                    Origin = S3BucketOrigin.WithOriginAccessControl(siteBucket),
                    ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                    CachePolicy = CachePolicy.CACHING_OPTIMIZED
                },
                DefaultRootObject = "index.html",
                ErrorResponses = new[]
                {
                    new ErrorResponse
                    {
                        HttpStatus         = 404,
                        ResponseHttpStatus = 200,
                        ResponsePagePath   = "/index.html"
                    }
                }
            });

            new CfnOutput(this, "SiteUrl", new CfnOutputProps
            {
                Value = $"https://{distribution.DistributionDomainName}"
            });
        }
    }
}