################################################################################

# Build stage (using Node 20)
ARG NODE_VERSION=20.12.2
FROM node:${NODE_VERSION}-alpine AS builder

WORKDIR /app

# Install dependencies only
COPY package*.json . 
RUN npm ci

# Copy the rest of the code and build the project
COPY . .
RUN npm run build

# Remove development dependencies
RUN npm prune --omit=dev

################################################################################

# Production stage (using the same Node version for consistency)
FROM node:${NODE_VERSION}-alpine

WORKDIR /app

# Copy built assets and production dependencies from builder
COPY --from=builder /app/build build/
COPY --from=builder /app/node_modules node_modules/
COPY package.json . 

# Expose the port your application will run on
EXPOSE 3000

# Set environment to production
ENV NODE_ENV=production

# Start the application (assuming `build` contains the entry point)
CMD ["node", "build"]
