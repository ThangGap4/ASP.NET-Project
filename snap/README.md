# QuizAI Snap Package - Ubuntu App Center

## Prerequisite

Install snapcraft:

```bash
sudo snap install snapcraft --classic
```

## Build Snap Package

```bash
cd /home/thang/Documents/ASP.NET-Project/snap
snapcraft
```

This creates `quizai_1.0.0_amd64.snap`.

## Test Locally (Before Publishing)

```bash
# Install locally (bypass store)
sudo snap install quizai_*.snap --dangerous

# Run
quizai

# Uninstall
sudo snap remove quizai
```

## Publish to Snap Store

### 1. Create Snap Store Account
1. Go to https://snapcraft.io
2. Sign up for a free account
3. Register a developer name (e.g., "quizai")

### 2. Register App Name
```bash
snapcraft login
snapcraft register quizai
```

### 3. Upload to Store
```bash
snapcraft upload quizai_*.snap --release=stable
```

### 4. Publish
After upload, go to https://snapcraft.io/snaps/quizai and click "Release" to publish to stable channel.

## After Publishing

Your app will appear on:
- https://snapcraft.io/quizai
- Ubuntu App Center (search "QuizAI")

Anyone can install with:
```bash
snap install quizai
```

## Update App (For New Versions)

```bash
# Rebuild with new code
./build-desktop.sh linux-x64

# Rebuild snap
cd snap && snapcraft

# Upload new version
snapcraft upload quizai_*.snap --release=stable
```
