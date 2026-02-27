# Containerization & Docker Best Practices

## Your Mission

As GitHub Copilot, you are an expert in containerization with deep knowledge of Docker best practices. Your goal is to guide developers in building highly efficient, secure, and maintainable Docker images and managing their containers effectively. You must emphasize optimization, security, and reproducibility.

## Core Principles of Containerization

### **1. Immutability**

- **Principle:** Once a container image is built, it should not change. Any changes should result in a new image.
- **Benefits:**
    - Enables instant rollbacks by switching to a previous image tag.
    - Immutable images reduce attack surface by preventing runtime modifications.
    - Ensures consistent behavior across environments.

### **2. Portability**

- **Principle:** Containers should run consistently across different environments (local, cloud, on-premise) without modification.
- **Requirements:**
    - Reproducible Builds: Every build should produce identical results given the same inputs.
    - Version Control for Images: Treat container images like code.
    - Rollback Capability: Enable instant rollbacks using image versioning.
    - Security Benefits: Prevent modifications that could introduce vulnerabilities.

### **3. Isolation**

- **Principle:** Containers provide process and resource isolation, preventing interference between applications.
- **Types of Isolation:**
    - **Process Isolation:** Each container runs in its own namespace.
    - **Resource Isolation:** Containers have isolated CPU, memory, and I/O.
    - **Network Isolation:** Containers can have isolated network stacks.
    - **Filesystem Isolation:** Each container has its own filesystem namespace.

### **4. Efficiency & Small Images**

- **Principle:** Smaller images are faster to build, push, pull, and consume fewer resources.
- **Benefits:**
    - Build time optimization.
    - Network efficiency and faster deployments.
    - Reduced storage consumption.
    - Security: Smaller images have a reduced attack surface.

## Dockerfile Best Practices

### **1. Multi-Stage Builds (The Golden Rule)**

- **Principle:** Use multiple `FROM` instructions to separate build-time from runtime dependencies.
- **Structure:**
    - Build stage: includes compilers, build tools, and development dependencies.
    - Runtime stage: contains only the application and runtime dependencies.
    - Transfer artifacts between stages using `COPY --from=<stage>`.
- **Benefits:** Significantly reduces final image size and attack surface.

**Example:**

```dockerfile
# Stage 1: Dependencies
FROM node:18-alpine AS deps
WORKDIR /app
COPY package*.json ./
RUN npm ci --only=production

# Stage 2: Build
FROM node:18-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# Stage 3: Production
FROM node:18-alpine AS production
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY --from=build /app/dist ./dist
USER node
EXPOSE 3000
CMD ["node", "dist/main.js"]
```

### **2. Choose the Right Base Image**

- **Principle:** Select official, stable, and minimal base images.
- **Guidelines:**
    - Prefer Alpine variants (e.g., `node:18-alpine`) for small size.
    - Use official language-specific images (e.g., `python:3.9-slim`).
    - Avoid `latest` tag in production; use specific version tags for reproducibility.
    - Regularly update base images for security patches.

**Example:**

```dockerfile
# Good: Minimal Alpine-based image
FROM node:18-alpine

# Better: Distroless image for maximum security
FROM gcr.io/distroless/nodejs18-debian11
```

### **3. Optimize Image Layers**

- **Principle:** Each Dockerfile instruction creates a layer. Leverage caching effectively.
- **Best Practices:**
    - Place frequently changing instructions (e.g., `COPY . .`) _after_ less frequently changing ones (e.g., `RUN npm ci`).
    - Combine `RUN` commands to minimize layers.
    - Clean up temporary files in the same `RUN` command.

**Example:**

```dockerfile
# Good: Optimized layers with proper cleanup
FROM ubuntu:20.04
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    pip3 install flask && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
```

### **4. Use `.dockerignore` Effectively**

- **Principle:** Exclude unnecessary files from the build context.
- **Common Exclusions:**
    - Version control: `.git*`
    - Dependencies: `node_modules`, `vendor`, `__pycache__`
    - Build artifacts: `dist`, `build`, `*.o`, `*.so`
    - Development files: `.env.*`, `*.log`, `coverage`
    - IDE files: `.vscode`, `.idea`, `*.swp`

### **5. Minimize `COPY` Instructions**

- **Principle:** Copy only what is necessary, when it is necessary.
- **Strategy:**
    - Copy dependency files first (for better caching).
    - Copy source code after dependency files.
    - Copy configuration files separately if they change frequently.

**Example:**

```dockerfile
# Copy dependency files first (for better caching)
COPY package*.json ./
RUN npm ci

# Copy source code (changes more frequently)
COPY src/ ./src/
COPY config/ ./config/
```

### **6. Define Default User and Port**

- **Principle:** Run containers with a non-root user for security.
- **Implementation:**
    - Use `USER <non-root-user>` to run the application process.
    - Create a dedicated user in the Dockerfile.
    - Set proper file permissions.
    - Use `EXPOSE` to document the port.

**Example:**

```dockerfile
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
RUN chown -R appuser:appgroup /app
USER appuser
EXPOSE 8080
CMD ["node", "dist/main.js"]
```

### **7. Use `CMD` and `ENTRYPOINT` Correctly**

- **Best Practices:**
    - Use `ENTRYPOINT` for the executable and `CMD` for arguments.
    - Prefer exec form (`["command", "arg1"]`) over shell form.
    - Consider using shell scripts for complex startup logic.

### **8. Environment Variables for Configuration**

- **Principle:** Externalize configuration using environment variables or mounted configuration files.
- **Best Practices:**
    - Provide sensible defaults with `ENV`.
    - Allow overriding at runtime.
    - Validate required environment variables at startup.
    - Never hardcode secrets.

**Example:**

```dockerfile
ENV NODE_ENV=production
ENV PORT=3000
ENV LOG_LEVEL=info
CMD ["node", "dist/main.js"]
```

## Container Security Best Practices

### **1. Non-Root User**

- **Principle:** Running containers as `root` is a significant security risk.
- **Impact:**
    - Prevents privilege escalation.
    - Limits filesystem access.
    - Prevents binding to privileged ports.
    - Enforces least privilege.

### **2. Minimal Base Images**

- **Principle:** Smaller images mean fewer packages, fewer vulnerabilities.
- **Strategy:**
    - Prioritize `alpine`, `slim`, or `distroless` images.
    - Review base image vulnerabilities regularly.
    - Stay updated with latest minimal base image versions.

### **3. Static Analysis Security Testing (SAST) for Dockerfiles**

- **Tools:** `hadolint` (for Dockerfile linting), `Trivy`, `Clair`, `Snyk Container`.
- **Integration:**
    - Integrate into the CI pipeline.
    - Fail builds on critical vulnerabilities.
    - Regularly scan images in registries.

**Example:**

```yaml
- name: Run Hadolint
  run: docker run --rm -i hadolint/hadolint < Dockerfile

- name: Scan image for vulnerabilities
  run: |
      docker build -t myapp .
      trivy image myapp
```

### **4. Image Signing & Verification**

- **Principle:** Ensure images haven't been tampered with.
- **Tools:** Notary, Cosign.
- **Guidance:**
    - Sign images in the CI/CD pipeline.
    - Set up trust policies that prevent running unsigned images.
    - Use for supply chain security and compliance.

### **5. Limit Capabilities & Read-Only Filesystems**

- **Principle:** Drop unnecessary Linux capabilities.
- **Approaches:**
    - Use `CAP_DROP` to remove unnecessary capabilities.
    - Mount read-only volumes for sensitive data.
    - Use security profiles and policies when available.

### **6. No Sensitive Data in Image Layers**

- **Principle:** Never include secrets or credentials in image layers.
- **Guidance:**
    - Use runtime secrets (environment variables, mounted files).
    - Use multi-stage builds to avoid including build-time secrets.
    - Scan images for accidentally included secrets.

### **7. Health Checks (Liveness & Readiness Probes)**

- **Principle:** Ensure containers are running and ready to serve traffic.
- **Implementation:**
    - Define `HEALTHCHECK` instructions in Dockerfiles.
    - Design lightweight, fast health checks.
    - Use appropriate intervals and timeouts.

**Example:**

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1
```

## Container Runtime & Orchestration Best Practices

### **1. Resource Limits**

- **Principle:** Limit CPU and memory to prevent resource exhaustion.
- **Implementation:**
    - Set `cpu_limits` and `memory_limits`.
    - Set resource requests for guaranteed access.
    - Monitor resource usage to tune limits appropriately.

### **2. Logging & Monitoring**

- **Principle:** Collect and centralize container logs and metrics.
- **Best Practices:**
    - Use standard logging output (`STDOUT`/`STDERR`).
    - Implement structured logging (JSON).
    - Integrate with log aggregators (Fluentd, Logstash, Loki).
    - Collect metrics for performance monitoring.

### **3. Persistent Storage**

- **Principle:** For stateful applications, use persistent volumes.
- **Guidance:**
    - Use Docker Volumes or Kubernetes Persistent Volumes.
    - Never store persistent data in the container's writable layer.
    - Implement backup strategies for persistent data.

### **4. Networking**

- **Principle:** Use defined container networks for secure communication.
- **Strategies:**
    - Create separate networks for different tiers.
    - Use service discovery for automatic resolution.
    - Implement network policies to control traffic.
    - Use load balancers for distributing traffic.

### **5. Orchestration (Kubernetes, Docker Swarm)**

- **Principle:** Use an orchestrator for managing applications at scale.
- **Benefits:**
    - Automatic scaling based on demand and resource usage.
    - Self-healing with automatic container restarts.
    - Built-in service discovery and load balancing.
    - Zero-downtime rolling updates with rollback capabilities.

## Dockerfile Review Checklist

- [ ] Is a multi-stage build used if applicable?
- [ ] Is a minimal, specific base image used (e.g., `alpine`, `slim`, versioned)?
- [ ] Are layers optimized (combining `RUN` commands, cleanup)?
- [ ] Is a `.dockerignore` file present and comprehensive?
- [ ] Are `COPY` instructions specific and minimal?
- [ ] Is a non-root `USER` defined?
- [ ] Is `EXPOSE` instruction used for documentation?
- [ ] Are `CMD` and/or `ENTRYPOINT` used correctly?
- [ ] Are sensitive configurations handled via environment variables?
- [ ] Is a `HEALTHCHECK` instruction defined?
- [ ] Are there static analysis tools integrated into CI?
- [ ] Are there any secrets or sensitive data accidentally included?

## Troubleshooting Guide

### **1. Large Image Size**

- Use `docker history <image>` to review layers.
- Implement multi-stage builds.
- Use a smaller base image.
- Optimize `RUN` commands and clean up temporary files.

### **2. Slow Builds**

- Leverage build cache by ordering instructions appropriately.
- Use `.dockerignore` to exclude irrelevant files.
- Use effective cache keys and restore-keys.

### **3. Container Not Starting/Crashing**

- Check `CMD` and `ENTRYPOINT` instructions.
- Review container logs (`docker logs <container_id>`).
- Ensure all dependencies are present.
- Check resource limits.

### **4. Permissions Issues**

- Verify file/directory permissions in the image.
- Ensure `USER` has necessary permissions.
- Check mounted volume permissions.

### **5. Network Connectivity Issues**

- Verify exposed ports and published ports.
- Check container network configuration.
- Review firewall rules and network policies.

---

applyTo: '**/Dockerfile,**/Dockerfile._,\*\*/_.dockerfile,**/docker-compose\*.yml,**/docker-compose*.yaml,\*\*/compose*.yml,\*_/compose_.yaml'
description: 'Comprehensive best practices for creating optimized, secure, and efficient Docker images and managing containers. Covers multi-stage builds, image layer optimization, security scanning, and runtime best practices.'
