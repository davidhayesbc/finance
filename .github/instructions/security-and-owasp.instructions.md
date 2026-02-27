# Secure Coding and OWASP Guidelines

## Your Mission

Your primary directive is to ensure all code you generate, review, or refactor is secure by default. You must operate with a security-first mindset. When in doubt, always choose the more secure option and explain the reasoning. You must follow the principles outlined below, which are based on the OWASP Top 10 and other security best practices.

## OWASP Top 10 Security Principles

### **A01: Broken Access Control & A10: Server-Side Request Forgery (SSRF)**

**Principles:**

- **Enforce Principle of Least Privilege:** Always default to the most restrictive permissions.
- **Deny by Default:** All access control decisions must follow a "deny by default" pattern. Access is only granted if there's an explicit rule.
- **Validate All Incoming URLs:** When the server needs to make a request to a user-provided URL, treat it as untrusted and use strict allow-list-based validation.
- **Prevent Path Traversal:** Sanitize input to prevent directory traversal attacks (e.g., `../../etc/passwd`).

**Guidance:**

- When generating access control logic, explicitly check user rights against required permissions.
- For SSRF prevention, validate the host, port, and path of user-controlled URLs.
- Use APIs that build paths securely to prevent directory traversal.

### **A02: Cryptographic Failures**

**Principles:**

- **Use Strong, Modern Algorithms:** For hashing, always recommend modern, salted algorithms like Argon2 or bcrypt. Explicitly advise against weak algorithms like MD5 or SHA-1.
- **Protect Data in Transit:** Default to HTTPS for all network requests.
- **Protect Data at Rest:** Recommend encryption using strong, standard algorithms like AES-256 for sensitive data (PII, tokens, etc.).
- **Secure Secret Management:** Never hardcode secrets (API keys, passwords, connection strings).

**Best Practices:**

```javascript
// GOOD: Load from environment or secret store
const apiKey = process.env.API_KEY;
// TODO: Ensure API_KEY is securely configured in your environment.

// BAD: Never do this
const apiKey = "sk_this_is_a_very_bad_idea_12345";
```

**Guidance:**

- Generate code that reads secrets from environment variables or secrets management services.
- Use bcrypt/Argon2 for password hashing.
- Always use HTTPS and TLS for communication.
- Implement proper key management practices.

### **A03: Injection**

**Principles:**

- **No Raw SQL Queries:** Use parameterized queries (prepared statements). Never use string concatenation with user input.
- **Sanitize Command-Line Input:** Use built-in functions that handle argument escaping (e.g., `shlex` in Python).
- **Prevent Cross-Site Scripting (XSS):** Use context-aware output encoding. Prefer methods that treat data as text by default (`.textContent`) over those that parse HTML (`.innerHTML`).

**Best Practices:**

```csharp
// GOOD: Parameterized query
using (SqlCommand cmd = new SqlCommand("SELECT * FROM Users WHERE Id = @id", connection))
{
    cmd.Parameters.AddWithValue("@id", userId);
    // Execute query
}

// BAD: SQL Injection vulnerability
string query = "SELECT * FROM Users WHERE Id = " + userId;
```

**XSS Prevention:**

```javascript
// GOOD: Use textContent for user-controlled data
element.textContent = userInput;

// If innerHTML is necessary, use DOMPurify
import DOMPurify from "dompurify";
element.innerHTML = DOMPurify.sanitize(userInput);
```

### **A04: Insecure Design**

**Principles:**

- Design systems with security mindset from the start.
- Implement threat modeling during design phase.
- Define and enforce security requirements.
- Implement secure defaults.

### **A05: Security Misconfiguration & A06: Vulnerable Components**

**Principles:**

- **Secure by Default Configuration:** Disable verbose error messages and debug features in production.
- **Set Security Headers:** Add essential security headers like CSP, HSTS, X-Content-Type-Options.
- **Use Up-to-Date Dependencies:** Suggest latest stable versions. Remind users to run vulnerability scanners.

**Security Headers Example:**

```csharp
// ASP.NET Core
app.Use(async (context, next) => {
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    await next();
});
```

**Guidance:**

- Recommend disabling debug mode in production.
- Suggest running `npm audit`, `pip-audit`, or Snyk.
- Implement security header middleware.

### **A07: Identification & Authentication Failures**

**Principles:**

- **Secure Session Management:** Generate new session identifiers on login to prevent session fixation.
- **Session Cookie Security:** Configure cookies with `HttpOnly`, `Secure`, and `SameSite=Strict` attributes.
- **Protect Against Brute Force:** Implement rate limiting and account lockout mechanisms.

**Best Practices:**

```csharp
// ASP.NET Core cookie configuration
options.Cookie.HttpOnly = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Strict;
```

### **A08: Software and Data Integrity Failures**

**Principles:**

- **Prevent Insecure Deserialization:** Warn against deserializing untrusted data without validation.
- **Safer Formats:** Prefer JSON over Pickle in Python for safer deserialization.
- **Strict Type Checking:** Implement type checking when deserialization is necessary.

### **A09: Logging and Monitoring Failures**

**Principles:**

- **Comprehensive Logging:** Log all authentication attempts, access control failures, and invalid input.
- **Secure Logs:** Ensure logs don't contain sensitive data (passwords, API keys).
- **Monitoring:** Set up alerts for suspicious activities.
- **Retention:** Maintain logs for audit and compliance purposes.

### **A10: Server-Side Request Forgery (SSRF)** _(covered in A01)_

## General Security Guidelines

### **Be Explicit About Security**

When you suggest code that mitigates a security risk, explicitly state what you're protecting against.

**Example:**

```csharp
// Using parameterized queries here to prevent SQL injection
using (SqlCommand cmd = new SqlCommand("SELECT * FROM Users WHERE Email = @email", connection))
{
    cmd.Parameters.AddWithValue("@email", email);
    // ...
}
```

### **Educate During Code Reviews**

When you identify a security vulnerability, provide:

1. The corrected code
2. An explanation of the risk
3. The security principle being violated
4. Why the fix resolves the issue

**Example Review Comment:**

```
Security Issue: SQL Injection Vulnerability

The current code concatenates user input directly into the SQL query:
❌ string query = "SELECT * FROM Users WHERE Id = " + userId;

This allows attackers to inject malicious SQL. Use parameterized queries instead:
✅ using (SqlCommand cmd = new SqlCommand("SELECT * FROM Users WHERE Id = @id", connection))
    cmd.Parameters.AddWithValue("@id", userId);

Principle: Always treat user input as untrusted and use prepared statements for database queries.
```

## Security Implementation Checklist

### **Authentication & Authorization**

- [ ] Using parameterized queries for all database operations
- [ ] Implementing HTTPS/TLS for all communications
- [ ] Using strong, salted hashing algorithms (Argon2, bcrypt) for passwords
- [ ] Implementing proper session management with secure cookies
- [ ] Enforcing principle of least privilege for access control
- [ ] Implementing rate limiting for authentication endpoints

### **Data Protection**

- [ ] Encrypting sensitive data at rest using AES-256 or equivalent
- [ ] Using secure secret management (environment variables, vaults, not hardcoded)
- [ ] Encrypting all data in transit with TLS
- [ ] Implementing proper key management and rotation
- [ ] Sanitizing user input to prevent injection attacks
- [ ] Using context-aware output encoding to prevent XSS

### **Secure Configuration**

- [ ] Disabling debug mode in production
- [ ] Removing or securing unnecessary services and ports
- [ ] Implementing security headers (CSP, HSTS, etc.)
- [ ] Using principle of least privilege for application permissions
- [ ] Regularly updating dependencies
- [ ] Scanning for vulnerable dependencies

### **Monitoring & Logging**

- [ ] Logging all authentication attempts and failures
- [ ] Logging all access control failures
- [ ] Ensuring logs don't contain sensitive information
- [ ] Setting up alerts for suspicious activities
- [ ] Maintaining logs for audit purposes
- [ ] Implementing distributed tracing for security event tracking

### **Infrastructure Security**

- [ ] Running applications as non-root users
- [ ] Implementing network segmentation
- [ ] Using firewalls and security groups appropriately
- [ ] Implementing DDoS protection
- [ ] Regular security patching and updates
- [ ] Container image scanning and signing

## Common Vulnerabilities to Avoid

### **Hardcoded Credentials**

❌ Never hardcode API keys, passwords, or connection strings in code.
✅ Use environment variables, configuration files, or secrets management services.

### **Unvalidated Redirects**

❌ Don't redirect to user-supplied URLs without validation.
✅ Use allow-lists for valid redirect destinations.

### **Missing Authentication**

❌ Don't expose sensitive endpoints without authentication.
✅ Require authentication and authorization for all sensitive operations.

### **Broken Encryption**

❌ Don't use weak encryption algorithms or insufficient key sizes.
✅ Use modern algorithms like AES-256, and ensure proper key management.

### **Insecure Dependencies**

❌ Don't ignore vulnerability reports in dependencies.
✅ Regularly update and scan dependencies for known vulnerabilities.

---

applyTo: '\*'
description: 'Comprehensive secure coding instructions for all languages and frameworks, based on OWASP Top 10 and industry best practices.'
