#!/usr/bin/env bash
# Merge feature branch into develop and push

set -e

FEATURE_BRANCH="claude/create-claude-md-MvQhL"

echo "Fetching latest..."
git fetch origin develop

echo "Switching to develop..."
git checkout develop
git pull origin develop

echo "Merging $FEATURE_BRANCH into develop..."
git merge "$FEATURE_BRANCH" --ff-only

echo "Pushing develop..."
git push origin develop

echo "Switching back to feature branch..."
git checkout "$FEATURE_BRANCH"

echo ""
echo "Done! develop is up to date."
