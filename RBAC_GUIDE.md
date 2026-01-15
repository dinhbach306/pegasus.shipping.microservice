# Role-Based Access Control (RBAC) Guide

## Overview

Pegasus.Ship implements **Permission-Based Authorization** using Auth0 as the identity provider. The API Gateway validates JWT tokens and extracts permissions, then forwards them to downstream services via headers.

## Architecture

```
┌─────────┐                 ┌──────────┐                ┌─────────┐
│ Client  │ ──JWT Token──>  │ Gateway  │ ──Permissions  │ Service │
│         │                 │ (Ocelot) │    in Headers   │         │
└─────────┘                 └──────────┘                └─────────┘
                                  │
                                  │ Validate JWT with Auth0
                                  │ Extract permissions claim
                                  │ Forward as X-User-Permissions
                                  ▼
                            X-User-Id: auth0|123
                            X-User-Email: user@example.com
                            X-User-Permissions: read:products,write:products
```

## Available Permissions

### Custom API Permissions (Your Auth0 API)

- `admin:all` - Full admin access to all resources
- `read:products` - Read shipments/products
- `write:products` - Create/update shipments/products
- `read:messages` - Read messages

### Auth0 Management API Permissions

- `read:users` - View user list and details
- `update:users` - Update user information
- `create:users` - Create new users
- `read:roles` - View roles
- `update:roles` - Update role information
- `create:roles` - Create new roles
- `read:role_members` - View role members
- `create:role_members` - Assign roles to users
- `delete:role_members` - Remove roles from users

## Setup in Auth0

### 1. Configure API Permissions

In your Auth0 Dashboard:

1. Go to **Applications** → **APIs** → Select your API
2. Go to **Permissions** tab
3. Add the following permissions:

```
admin:all         - Full admin access
read:products     - Read products
write:products    - Write products
read:messages     - Read messages
```

### 2. Create Roles

1. Go to **User Management** → **Roles**
2. Create roles:

**Admin Role:**

```json
{
  "name": "Admin",
  "description": "Full system access",
  "permissions": ["admin:all"]
}
```

**Product Manager Role:**

```json
{
  "name": "Product Manager",
  "description": "Manage products and shipments",
  "permissions": ["read:products", "write:products"]
}
```

**User Role:**

```json
{
  "name": "User",
  "description": "Basic user access",
  "permissions": ["read:products", "read:messages"]
}
```

### 3. Assign Roles to Users

**Option A: Via Auth0 Dashboard**

1. Go to **User Management** → **Users**
2. Select a user
3. Go to **Roles** tab
4. Assign roles

**Option B: Via API (using Admin endpoints)**

```bash
# Get token with admin:all permission
curl -X POST http://localhost:5100/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"AdminPass123!"}'

# Assign role to user
curl -X POST http://localhost:5100/api/admin/users/{userId}/roles \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"roles":["rol_xxxxx"]}'
```

### 4. Configure Auth0 to Include Permissions in Token

Create an **Action** in Auth0:

1. Go to **Actions** → **Flows** → **Login**
2. Create Custom Action:

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = "https://your-api.com";

  if (event.authorization) {
    // Add permissions to access token
    api.accessToken.setCustomClaim(
      `permissions`,
      event.authorization.permissions
    );

    // Optionally add roles
    api.accessToken.setCustomClaim(
      `${namespace}/roles`,
      event.authorization.roles
    );
  }
};
```

3. **Deploy** and add to Login flow

## Using Permissions in Code

### 1. Controller-Level Authorization

```csharp
using SharedKernel;

[ApiController]
[Route("api/shipments")]
public class ShipmentsController : ControllerBase
{
    // Requires read:products OR admin:all
    [RequirePermission("read:products", "admin:all")]
    [HttpGet("{id}")]
    public IActionResult Get(string id) { }

    // Requires write:products OR admin:all
    [RequirePermission("write:products", "admin:all")]
    [HttpPost]
    public IActionResult Create() { }

    // Requires ONLY admin:all
    [RequirePermission("admin:all")]
    [HttpDelete("{id}")]
    public IActionResult Delete(string id) { }
}
```

### 2. Programmatic Permission Checks

```csharp
public class ShipmentsController(HeaderUserContext userContext) : ControllerBase
{
    [HttpGet("sensitive")]
    public IActionResult GetSensitiveData()
    {
        // Check single permission
        if (!userContext.HasPermission("admin:all"))
        {
            return Forbid();
        }

        // Check multiple permissions (OR logic)
        if (!userContext.HasAnyPermission("admin:all", "read:products"))
        {
            return Forbid();
        }

        return Ok(new { data = "sensitive" });
    }
}
```

### 3. Access User Context

```csharp
public class MyController(HeaderUserContext userContext) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var userId = userContext.UserId;
        var email = userContext.Email;
        var permissions = userContext.Permissions; // string[]
        var isAuthenticated = userContext.IsAuthenticated;

        return Ok(new { userId, email, permissions });
    }
}
```

## API Examples

### Test Authentication

```bash
# 1. Login and get token
curl -X POST http://localhost:5100/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!"
  }'

# Response:
{
  "accessToken": "eyJ0eXAi...",
  "tokenType": "Bearer",
  "expiresIn": 86400
}
```

### User Endpoints (read:products permission)

```bash
# Get shipment - Requires read:products OR admin:all
curl -X GET http://localhost:5100/api/shipments/TRACK123 \
  -H "Authorization: Bearer USER_TOKEN"

# Response with permissions:
{
  "shipment": { ... },
  "requestedBy": {
    "userId": "auth0|123",
    "email": "user@example.com",
    "permissions": ["read:products", "read:messages"]
  }
}
```

### Product Manager Endpoints (write:products permission)

```bash
# Create shipment - Requires write:products OR admin:all
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer PRODUCT_MANAGER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"trackingNumber":"TRACK456"}'
```

### Admin Endpoints (admin:all permission)

```bash
# Admin test
curl -X GET http://localhost:5100/api/admin/test \
  -H "Authorization: Bearer ADMIN_TOKEN"

# Get all users - Requires admin:all OR read:users
curl -X GET http://localhost:5100/api/admin/users \
  -H "Authorization: Bearer ADMIN_TOKEN"

# Get user details
curl -X GET http://localhost:5100/api/admin/users/auth0|123456 \
  -H "Authorization: Bearer ADMIN_TOKEN"

# Get all roles
curl -X GET http://localhost:5100/api/admin/roles \
  -H "Authorization: Bearer ADMIN_TOKEN"

# Assign role to user
curl -X POST http://localhost:5100/api/admin/users/auth0|123456/roles \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"roles":["rol_xxxxx"]}'

# Remove role from user
curl -X DELETE http://localhost:5100/api/admin/users/auth0|123456/roles \
  -H "Authorization: Bearer ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"roles":["rol_xxxxx"]}'

# Delete shipment - Requires admin:all ONLY
curl -X DELETE http://localhost:5100/api/shipments/TRACK123 \
  -H "Authorization: Bearer ADMIN_TOKEN"
```

## Error Responses

### 401 Unauthorized (No token or invalid token)

```json
{
  "error": "Unauthorized",
  "message": "No authentication token provided"
}
```

### 403 Forbidden (Insufficient permissions)

```json
{
  "error": "Forbidden",
  "message": "You do not have permission to access this resource"
}
```

## Testing RBAC

### 1. Create Test Users in Auth0

```bash
# Admin user
Email: admin@test.com
Password: Admin123!
Roles: Admin
Permissions: admin:all

# Product Manager user
Email: manager@test.com
Password: Manager123!
Roles: Product Manager
Permissions: read:products, write:products

# Regular user
Email: user@test.com
Password: User123!
Roles: User
Permissions: read:products, read:messages
```

### 2. Login as Each User

```bash
# Login as admin
ADMIN_TOKEN=$(curl -s -X POST http://localhost:5100/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@test.com","password":"Admin123!"}' \
  | jq -r '.accessToken')

# Login as manager
MANAGER_TOKEN=$(curl -s -X POST http://localhost:5100/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"manager@test.com","password":"Manager123!"}' \
  | jq -r '.accessToken')

# Login as user
USER_TOKEN=$(curl -s -X POST http://localhost:5100/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@test.com","password":"User123!"}' \
  | jq -r '.accessToken')
```

### 3. Test Permission Boundaries

```bash
# User CAN read products
curl -X GET http://localhost:5100/api/shipments/TRACK123 \
  -H "Authorization: Bearer $USER_TOKEN"
# ✅ Success

# User CANNOT create products
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"trackingNumber":"TRACK789"}'
# ❌ 403 Forbidden

# Manager CAN create products
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer $MANAGER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"trackingNumber":"TRACK789"}'
# ✅ Success

# Manager CANNOT delete products
curl -X DELETE http://localhost:5100/api/shipments/TRACK789 \
  -H "Authorization: Bearer $MANAGER_TOKEN"
# ❌ 403 Forbidden

# Admin CAN delete products
curl -X DELETE http://localhost:5100/api/shipments/TRACK789 \
  -H "Authorization: Bearer $ADMIN_TOKEN"
# ✅ Success

# User CANNOT access admin endpoints
curl -X GET http://localhost:5100/api/admin/users \
  -H "Authorization: Bearer $USER_TOKEN"
# ❌ 403 Forbidden

# Admin CAN access admin endpoints
curl -X GET http://localhost:5100/api/admin/users \
  -H "Authorization: Bearer $ADMIN_TOKEN"
# ✅ Success
```

## Best Practices

1. **Principle of Least Privilege**: Grant minimum permissions needed
2. **Use Roles for Groups**: Group permissions into roles
3. **Check Permissions at Gateway**: JWT validation happens once at Gateway
4. **Services Trust Gateway**: Downstream services read from headers
5. **Audit Permissions**: Log all permission checks in production
6. **Token Expiry**: Tokens expire in 24 hours by default
7. **Refresh Tokens**: Implement refresh token flow for long-lived sessions

## Troubleshooting

### Permissions not in token

- Check Auth0 Action is deployed and in Login flow
- Verify permissions are assigned to role in Auth0
- Ensure role is assigned to user

### 403 Forbidden despite having permission

- Check permission name matches exactly (case-sensitive)
- Verify `X-User-Permissions` header is forwarded by Gateway
- Check Ocelot route has `AddHeadersToRequest` with permissions

### Gateway not forwarding permissions

- Ensure Auth0 token includes `permissions` claim
- Check Ocelot config: `"X-User-Permissions": "Claims[permissions] > value"`
- Verify token has not expired

## Production Considerations

- Use **Auth0 Organizations** for multi-tenancy
- Implement **fine-grained permissions** (e.g., `read:products:own` vs `read:products:all`)
- Add **rate limiting** per permission level
- Enable **audit logging** for all admin actions
- Use **encrypted headers** between Gateway and services
- Implement **permission caching** in services for performance
