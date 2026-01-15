# RBAC Quick Reference

## Available Permissions

### Your Custom API Permissions
| Permission | Description | Example Usage |
|-----------|-------------|---------------|
| `admin:all` | Full admin access | Admin endpoints, delete operations |
| `read:products` | Read shipments/products | GET /api/shipments/{id} |
| `write:products` | Create/update products | POST /api/shipments |
| `read:messages` | Read messages | Future messaging feature |

### Auth0 Management API Permissions
| Permission | Description | Admin Endpoint |
|-----------|-------------|----------------|
| `read:users` | View users | GET /api/admin/users |
| `update:users` | Update users | PUT /api/admin/users/{id} |
| `create:users` | Create users | POST /api/admin/users |
| `read:roles` | View roles | GET /api/admin/roles |
| `update:roles` | Update roles | PUT /api/admin/roles/{id} |
| `create:roles` | Create roles | POST /api/admin/roles |
| `read:role_members` | View role members | GET /api/admin/roles/{id}/members |
| `create:role_members` | Assign roles | POST /api/admin/users/{id}/roles |
| `delete:role_members` | Remove roles | DELETE /api/admin/users/{id}/roles |

## Suggested Roles

### Admin
```json
{
  "name": "Admin",
  "permissions": ["admin:all"]
}
```
**Can access:** Everything

### Product Manager
```json
{
  "name": "Product Manager",
  "permissions": ["read:products", "write:products"]
}
```
**Can access:** Read and create shipments

### Support Agent
```json
{
  "name": "Support Agent",
  "permissions": ["read:products", "read:messages", "read:users"]
}
```
**Can access:** View shipments, messages, and user info

### User
```json
{
  "name": "User",
  "permissions": ["read:products", "read:messages"]
}
```
**Can access:** Basic read operations

## API Endpoints by Permission

### Public (No Permission Required)
```bash
GET  /api/shipments/status
GET  /api/identity/login
POST /api/identity/register
```

### read:products (or admin:all)
```bash
GET /api/shipments/{trackingNumber}
```

### write:products (or admin:all)
```bash
POST /api/shipments
```

### admin:all ONLY
```bash
DELETE /api/shipments/{trackingNumber}
GET    /api/admin/test
```

### read:users (or admin:all)
```bash
GET /api/admin/users
GET /api/admin/users/{userId}
```

### read:roles (or admin:all)
```bash
GET /api/admin/roles
```

### create:role_members (or admin:all)
```bash
POST /api/admin/users/{userId}/roles
```

### delete:role_members (or admin:all)
```bash
DELETE /api/admin/users/{userId}/roles
```

## Code Examples

### Protect Endpoint
```csharp
[RequirePermission("read:products", "admin:all")]
[HttpGet("{id}")]
public IActionResult Get(string id) { }
```

### Check Permission Programmatically
```csharp
if (!userContext.HasPermission("admin:all"))
{
    return Forbid();
}
```

### Access User Info with Permissions
```csharp
var userId = userContext.UserId;
var email = userContext.Email;
var permissions = userContext.Permissions; // string[]
```

## Quick Setup Checklist

- [ ] Create API in Auth0 with permissions
- [ ] Create Auth0 Action to add permissions to token
- [ ] Create roles in Auth0 with permission assignments
- [ ] Assign roles to users
- [ ] Test with `curl` using different user tokens
- [ ] Verify 403 Forbidden when permissions missing

## Common Issues

**403 Forbidden despite correct permission:**
- Check permission spelling (case-sensitive)
- Verify Auth0 Action is deployed
- Ensure `X-User-Permissions` header is forwarded

**Empty permissions array:**
- Check Auth0 Action includes `api.accessToken.setCustomClaim('permissions', ...)`
- Verify role has permissions assigned
- Ensure user has role assigned

**Token doesn't include permissions:**
- Auth0 Action not in Login flow
- Action not deployed
- Permissions not added to access token (only ID token)

## Test Commands

```bash
# Get tokens
ADMIN_TOKEN=$(curl -s POST http://localhost:5100/api/identity/login -d '{"email":"admin@test.com","password":"Admin123!"}' | jq -r '.accessToken')
USER_TOKEN=$(curl -s POST http://localhost:5100/api/identity/login -d '{"email":"user@test.com","password":"User123!"}' | jq -r '.accessToken')

# Test admin endpoint
curl http://localhost:5100/api/admin/test -H "Authorization: Bearer $ADMIN_TOKEN"
# ✅ Success

curl http://localhost:5100/api/admin/test -H "Authorization: Bearer $USER_TOKEN"
# ❌ 403 Forbidden

# Test create shipment
curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer $USER_TOKEN" \
  -d '{"trackingNumber":"T123"}'
# ❌ 403 Forbidden (user doesn't have write:products)

curl -X POST http://localhost:5100/api/shipments \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"trackingNumber":"T123"}'
# ✅ Success (admin has admin:all)
```

## See Full Documentation

- `RBAC_GUIDE.md` - Complete RBAC guide with setup steps
- `README.md` - System overview
- `ARCHITECTURE.md` - Architecture details

