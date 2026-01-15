# Auth0 Setup Guide

## Overview

This guide walks you through setting up Auth0 for the Identity service, including user registration, login, and social authentication.

## Prerequisites

- Auth0 account (sign up at https://auth0.com)
- Access to Auth0 Dashboard

## Step-by-Step Setup

### 1. Create Auth0 Tenant

1. Go to https://auth0.com and create an account
2. Create a new tenant (e.g., `your-company`)
3. Note your domain: `your-company.us.auth0.com` or `your-company.jp.auth0.com`

### 2. Create API

This represents your backend API.

1. Go to **Applications** → **APIs**
2. Click **Create API**
3. Fill in:
   - **Name**: `Pegasus Ship API` (or your API name)
   - **Identifier**: `https://pegasus-ship-api` (this is your **Audience**)
   - **Signing Algorithm**: `RS256`
4. Click **Create**

#### Add Permissions (Scopes)

1. Go to **Permissions** tab
2. Add the following permissions:

| Permission        | Description             |
| ----------------- | ----------------------- |
| `read:messages`   | Read messages           |
| `admin:all`       | Full admin access       |
| `read:products`   | Read products           |
| `write:products`  | Create/update products  |
| `read:shipments`  | Read shipments          |
| `write:shipments` | Create/update shipments |

3. Click **Save**

### 3. Create Application

This represents your backend service that will authenticate users.

1. Go to **Applications** → **Applications**
2. Click **Create Application**
3. Fill in:
   - **Name**: `Identity Service`
   - **Application Type**: **Regular Web Application**
4. Click **Create**

#### Configure Application

1. Go to **Settings** tab
2. Note down (you'll need these in `appsettings.json`):

   - **Domain**: `your-tenant.us.auth0.com`
   - **Client ID**: `abc123...`
   - **Client Secret**: `xyz789...` (click **Show** to reveal)

3. Scroll down to **Application URIs**:

   - **Allowed Callback URLs**: `http://localhost:5117/api/identity/google/callback, http://localhost:5100/api/identity/google/callback`
   - **Allowed Logout URLs**: `http://localhost:3000`
   - **Allowed Web Origins**: `http://localhost:3000`

4. Scroll down to **Advanced Settings** → **Grant Types**

   - ✅ Enable: `Authorization Code`
   - ✅ Enable: `Refresh Token`
   - ✅ Enable: `Password` (for username/password login)

5. Click **Save Changes**

### 4. Enable Database Connection

**This is critical to fix the 403 error!**

1. Go to **Applications** → **Applications** → Your Application (`Identity Service`)
2. Go to **Connections** tab
3. Under **Database**, enable **Username-Password-Authentication**
4. Click the **toggle** to enable it

**Verify:**

- ✅ `Username-Password-Authentication` should show as **enabled** (green toggle)

### 5. Configure Database Connection

1. Go to **Authentication** → **Database**
2. Click on **Username-Password-Authentication**
3. In **Settings** tab:

   - ✅ **Requires Username**: OFF (we use email as username)
   - ✅ **Requires Email Verification**: OFF (for development; enable in production)
   - **Username Length**: min 1, max 50

4. In **Password Policy** tab:

   - Choose: `Good` (min 8 characters, lowercase, uppercase, number)
   - Or adjust based on your requirements

5. In **Applications** tab:

   - ✅ Ensure your application (`Identity Service`) is **enabled**

6. Click **Save**

### 6. Enable Google Social Login (Optional)

1. Go to **Authentication** → **Social**
2. Click **Create Connection**
3. Select **Google**

#### Get Google OAuth Credentials

1. Go to https://console.cloud.google.com
2. Create a new project (or select existing)
3. Go to **APIs & Services** → **Credentials**
4. Click **Create Credentials** → **OAuth 2.0 Client ID**
5. Configure OAuth consent screen if prompted
6. Application type: **Web application**
7. Add **Authorized redirect URIs**:
   ```
   https://your-tenant.us.auth0.com/login/callback
   ```
8. Copy **Client ID** and **Client Secret**

#### Configure in Auth0

1. Back in Auth0, paste Google's **Client ID** and **Client Secret**
2. In **Applications** tab, enable your application (`Identity Service`)
3. Click **Save**

### 7. Create Machine-to-Machine Application (for Admin Operations)

This is for Management API access (user/role management).

1. Go to **Applications** → **Applications**
2. Click **Create Application**
3. Fill in:
   - **Name**: `Identity Service - Management`
   - **Application Type**: **Machine to Machine Applications**
4. Select API: **Auth0 Management API**
5. Click **Authorize**

#### Grant Permissions

Select the following scopes:

**User Management:**

- ✅ `read:users`
- ✅ `create:users`
- ✅ `update:users`
- ✅ `delete:users`

**Role Management:**

- ✅ `read:roles`
- ✅ `create:roles`
- ✅ `update:roles`
- ✅ `delete:roles`
- ✅ `read:role_members`
- ✅ `create:role_members`
- ✅ `delete:role_members`

**Permissions:**

- ✅ `read:permissions`

6. Click **Authorize**
7. Note **Client ID** and **Client Secret** for Management API

### 8. Update appsettings.json

Update `src/Services/Identity/Identity.Api/appsettings.json`:

```json
{
  "Auth0": {
    "Domain": "your-tenant.us.auth0.com",
    "Audience": "https://pegasus-ship-api",
    "ClientId": "YOUR_APPLICATION_CLIENT_ID",
    "ClientSecret": "YOUR_APPLICATION_CLIENT_SECRET",
    "Connection": "Username-Password-Authentication",
    "ManagementApi": {
      "ClientId": "YOUR_M2M_CLIENT_ID",
      "ClientSecret": "YOUR_M2M_CLIENT_SECRET"
    }
  }
}
```

**Important:**

- `Domain`: Your Auth0 tenant domain
- `Audience`: API Identifier from Step 2
- `ClientId`: From Regular Web Application (Step 3)
- `ClientSecret`: From Regular Web Application (Step 3)
- `Connection`: Database connection name (usually `Username-Password-Authentication`)
- `ManagementApi.ClientId`: From M2M Application (Step 7)
- `ManagementApi.ClientSecret`: From M2M Application (Step 7)

### 9. Update API Gateway appsettings.json

Update `src/ApiGateway/appsettings.json`:

```json
{
  "Auth0": {
    "Domain": "your-tenant.us.auth0.com",
    "Audience": "https://pegasus-ship-api"
  }
}
```

### 10. Update Shipping Service appsettings.json

Update `src/Services/Shipping/Shipping.Api/appsettings.json`:

```json
{
  "Auth0": {
    "Domain": "your-tenant.us.auth0.com",
    "Audience": "https://pegasus-ship-api"
  }
}
```

## Testing the Setup

### 1. Test User Registration

```bash
curl -X POST http://localhost:5117/api/identity/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test1234!",
    "name": "Test User"
  }'
```

**Expected Response:**

```json
{
  "accessToken": "eyJ...",
  "idToken": "eyJ...",
  "tokenType": "Bearer",
  "expiresIn": 86400
}
```

**If you get 403 error:**

- ✅ Check that Database Connection is enabled for your Application (Step 4)
- ✅ Verify `ClientId` and `ClientSecret` are correct
- ✅ Ensure `Connection` name matches exactly

### 2. Test Login

```bash
curl -X POST http://localhost:5117/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test1234!"
  }'
```

### 3. Test Google Login

```bash
# Get Google login URL
curl http://localhost:5117/api/identity/google-login-url?redirectUri=http://localhost:5117/api/identity/google/callback

# Open the returned URL in browser
# After Google authentication, you'll be redirected with a code
# The callback endpoint will exchange code for tokens
```

### 4. Test Protected Endpoint

```bash
# Save access token from login response
TOKEN="eyJ..."

# Call protected endpoint
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "trackingNumber": "TRACK123"
  }'
```

## Troubleshooting

### Error: 403 Forbidden on Register

**Cause:** Database Connection not enabled for Application

**Fix:**

1. Go to **Applications** → Your Application → **Connections** tab
2. Enable **Username-Password-Authentication**
3. Save and retry

### Error: 401 Unauthorized on Login

**Cause:**

- Password grant not enabled
- Wrong ClientId/ClientSecret
- Wrong Audience

**Fix:**

1. Check **Grant Types** in Application Settings → Advanced Settings
2. Verify `ClientId`, `ClientSecret`, `Audience` in appsettings.json
3. Ensure they match Auth0 Dashboard values

### Error: invalid_grant

**Cause:** User doesn't exist or wrong password

**Fix:**

- Register user first
- Verify email/password are correct
- Check Auth0 Dashboard → **User Management** → **Users** to see if user exists

### Error: Access denied

**Cause:** User doesn't have required permissions

**Fix:**

1. Go to **User Management** → **Users** → Select user
2. Go to **Roles** tab
3. Assign appropriate roles
4. Or use Admin API to assign roles programmatically

### Google Login Not Working

**Cause:**

- Google connection not enabled
- Wrong redirect URI
- Google OAuth not configured

**Fix:**

1. Verify Google connection is enabled in **Authentication** → **Social**
2. Check redirect URI in Google Console matches Auth0 callback
3. Ensure your application has Google connection enabled

## Security Best Practices

### Production Settings

1. **Enable Email Verification:**

   - Database Connection → Settings → Requires Email Verification: ON

2. **Strong Password Policy:**

   - Database Connection → Password Policy → Excellent or Fair

3. **Use Environment Variables:**

   ```bash
   export AUTH0_DOMAIN="your-tenant.us.auth0.com"
   export AUTH0_CLIENT_SECRET="your-secret"
   ```

   Never commit secrets to git!

4. **Restrict Allowed Origins:**

   - Only add production URLs
   - Remove `localhost` in production

5. **Enable MFA (Multi-Factor Authentication):**

   - Security → Multi-factor Auth → Enable

6. **Rate Limiting:**
   - Auth0 automatically rate limits
   - Monitor in Dashboard → Monitoring → Logs

### Token Best Practices

1. **Short-lived Access Tokens:**

   - API Settings → Token Expiration: 86400 seconds (24 hours) or less

2. **Use Refresh Tokens:**

   - Enable Refresh Token grant in Application settings

3. **Validate Audience:**

   - Always check `aud` claim matches your API

4. **Store Tokens Securely:**
   - Use HttpOnly cookies or secure storage
   - Never store in localStorage if possible

## Auth0 Dashboard Quick Links

| Task                 | Location                               |
| -------------------- | -------------------------------------- |
| View Users           | User Management → Users                |
| View Logs            | Monitoring → Logs                      |
| API Settings         | Applications → APIs → Your API         |
| Application Settings | Applications → Applications → Your App |
| Database Connection  | Authentication → Database              |
| Social Connections   | Authentication → Social                |
| Roles & Permissions  | User Management → Roles                |

## Common Auth0 Endpoints

| Endpoint                                | Purpose              |
| --------------------------------------- | -------------------- |
| `https://{domain}/dbconnections/signup` | User registration    |
| `https://{domain}/oauth/token`          | Get access token     |
| `https://{domain}/authorize`            | OAuth2 authorization |
| `https://{domain}/userinfo`             | Get user profile     |
| `https://{domain}/api/v2/`              | Management API       |

## Common Issue: Claim Type Mapping in API Gateway

### Problem

When using Ocelot API Gateway with JWT Bearer authentication, you might see errors like:

```
CannotFindClaimError: Cannot find claim for key: sub
```

Even though the JWT token contains the `sub` claim, .NET automatically converts it to a long XML schema name:

```
http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
```

### Solution

Clear the default claim type mappings in your API Gateway's `Program.cs`:

```csharp
// Add BEFORE AddAuthentication()
Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication("Auth0")
    .AddJwtBearer("Auth0", options =>
    {
        var domain = builder.Configuration["Auth0:Domain"];
        options.Authority = $"https://{domain}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "sub" // Use 'sub' as the name identifier
        };
    });
```

**Why this works:**

- `JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear()` prevents .NET from converting JWT claims to XML schema names
- This keeps original claim names like `sub`, `email`, `permissions` intact
- Ocelot can now correctly map `Claims[sub] > value` to `X-User-Id` header

### Affected Claims

Without clearing the map, these JWT claims are converted:

| Original JWT Claim | .NET Converted Name                                                    |
| ------------------ | ---------------------------------------------------------------------- |
| `sub`              | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` |
| `email`            | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`   |
| `name`             | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`           |

After clearing, claims retain their original names from the JWT token.

## References

- [Auth0 Documentation](https://auth0.com/docs)
- [Auth0 .NET SDK](https://auth0.com/docs/quickstart/backend/aspnet-core-webapi)
- [Auth0 Management API](https://auth0.com/docs/api/management/v2)
- [Resource Owner Password Flow](https://auth0.com/docs/get-started/authentication-and-authorization-flow/resource-owner-password-flow)
- [JWT Claim Type Mapping](https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtSecurityTokenHandler.defaultinboundclaimtypemap)

## Support

If you encounter issues:

1. Check Auth0 Dashboard → **Monitoring** → **Logs** for detailed error messages
2. Enable debug logging in your application
3. Verify all settings match this guide
4. Check Auth0 Community: https://community.auth0.com
