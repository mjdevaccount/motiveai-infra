# MotiveAI Infra

AWS CDK infrastructure for MotiveAI — written entirely in C# (.NET 8).

> Application code lives in [motiveai-app](https://github.com/mjdevaccount/motiveai-app)

---

## What This Deploys

Three independent CloudFormation stacks, each with a clear responsibility:

| Stack | Resources |
|---|---|
| `MotiveAI-Auth` | Cognito User Pool, App Client, Hosted UI domain |
| `MotiveAI-Api` | API Gateway (REST), Lambda function, Secrets Manager |
| `MotiveAI-Frontend` | S3 bucket (private), CloudFront distribution, OAC |

---

## Architecture

```
[Cognito User Pool]  ──  invite-only auth, SRP flow, hosted UI
        ↓
[API Gateway]  ──  REST API, CORS configured
        ↓
[Lambda (.NET 8)]  ──  motiveai-analyze function
        ↓
[Secrets Manager]  ──  Claude API key, never in code
        ↓
[S3 + CloudFront]  ──  private bucket, HTTPS enforced, global CDN
```

---

## Stack Details

### MotiveAI-Auth
- Invite-only User Pool (self-signup disabled)
- Email-based sign-in with SRP authentication
- OAuth 2.0 authorization code grant
- Hosted UI at `motiveai-auth.auth.us-east-1.amazoncognito.com`
- 1-hour access/ID tokens, 30-day refresh
- `RemovalPolicy.RETAIN` — user accounts survive stack updates

### MotiveAI-Api
- API Gateway REST API with CORS
- Lambda function running .NET 8 on `linux-x64`
- Secrets Manager secret for Claude API key (value set post-deploy)
- IAM role scoped to read only its specific secret

### MotiveAI-Frontend
- Fully private S3 bucket (no public access)
- CloudFront with Origin Access Control (OAC)
- HTTPS enforced, HTTP redirected
- SPA routing — 404s return `index.html` with 200
- Cache invalidation on deploy

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [AWS CDK v2](https://docs.aws.amazon.com/cdk/v2/guide/getting_started.html)
- AWS CLI configured with appropriate permissions

```bash
npm install -g aws-cdk
aws configure
```

---

## First Time Setup

```bash
# Bootstrap CDK in your AWS account (one time only)
cdk bootstrap aws://YOUR_ACCOUNT_ID/us-east-1

# Deploy all stacks
cdk deploy --all
```

---

## Post-Deploy Configuration

### Set Claude API Key

```bash
aws secretsmanager put-secret-value \
  --secret-id motiveai/claude-api-key \
  --secret-string "sk-ant-YOUR-KEY-HERE"
```

### Create Your First User

```bash
aws cognito-idp admin-create-user \
  --user-pool-id YOUR_USER_POOL_ID \
  --username user@example.com \
  --message-action SUPPRESS

aws cognito-idp admin-set-user-password \
  --user-pool-id YOUR_USER_POOL_ID \
  --username user@example.com \
  --password YourPassword123! \
  --permanent
```

---

## Useful Commands

```bash
cdk diff           # preview changes before deploying
cdk deploy --all   # deploy all stacks
cdk deploy MotiveAI-Auth    # deploy individual stack
cdk destroy --all  # tear everything down
cdk synth          # emit CloudFormation templates
```

---

## Cost Estimate

| Service | Est. Monthly |
|---|---|
| CloudFront | ~$1-2 |
| S3 | < $1 |
| Lambda | Free tier |
| API Gateway | Free tier |
| Cognito | Free tier (< 50k MAU) |
| Secrets Manager | ~$0.40/secret |
| **Total** | **~$5-10/month** |

Tear down with `cdk destroy --all` when not in use to eliminate costs entirely.

---

## Security Notes

- Root account not used — dedicated IAM user with programmatic access only
- Secrets Manager used for all sensitive values — no credentials in code or CDK outputs
- CloudFront OAC replaces legacy OAI — bucket has zero public access
- Cognito `RemovalPolicy.RETAIN` prevents accidental user deletion on stack updates

---

## Tech Stack

**C# / .NET 8** · **AWS CDK v2** · **CloudFormation** · **AWS Lambda** · **Amazon Cognito** · **CloudFront** · **Secrets Manager**

---

*Application code in [motiveai-app](https://github.com/mjdevaccount/motiveai-app)*
