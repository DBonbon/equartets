#!/bin/bash
# Usage: ./sync-public.sh branch-name
# Pushes branch snapshot (no history) to public repo main branch.
# validate: chmod +x sync-public.sh
set -e

PUBLIC_REMOTE_NAME="public-origin"
PUBLIC_REPO_URL="git@github.com:DBonbon/equartets.git"
BRANCH=$1

if [ -z "$BRANCH" ]; then
    echo "Usage: $0 branch-name"
    echo "Example: $0 release"
    exit 1
fi

# Add remote if missing
if ! git remote | grep -q "^$PUBLIC_REMOTE_NAME$"; then
    echo "🔗 Adding remote '$PUBLIC_REMOTE_NAME' -> $PUBLIC_REPO_URL"
    git remote add "$PUBLIC_REMOTE_NAME" "$PUBLIC_REPO_URL"
fi

# Checkout target branch
git checkout "$BRANCH"

# Create a temp workdir from branch contents
TMPDIR=$(mktemp -d)
git archive "$BRANCH" | tar -x -C "$TMPDIR"

# Push to public repo
cd "$TMPDIR"
git init
git checkout -b main
git add .
git commit -m "Public snapshot from branch '$BRANCH'"
git remote add origin "$PUBLIC_REPO_URL"
git push origin main --force

# Clean up
cd -
rm -rf "$TMPDIR"

echo "✅ Public repo updated from branch '$BRANCH' to '$PUBLIC_REMOTE_NAME/main'."
