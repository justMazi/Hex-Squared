# Stage 1: Build
ARG NODE_VERSION=20.12.2
FROM --platform=linux/arm64 node:${NODE_VERSION}-alpine AS builder

# Set the working directory
WORKDIR /app

# Install dependencies
COPY package*.json .
RUN npm ci

# Copy the rest of the code and build the project
COPY . .
RUN npm run build

# Remove development dependencies
RUN npm prune --omit=dev

# Stage 2: Runtime
FROM --platform=linux/arm64 node:${NODE_VERSION}-alpine

# Set the working directory
WORKDIR /app

# Copy built assets and production dependencies from builder
COPY --from=builder /app/build build/
COPY --from=builder /app/node_modules node_modules/
COPY package.json .

# Expose the port
EXPOSE 3000

# Start the application
CMD ["node", "build"]
