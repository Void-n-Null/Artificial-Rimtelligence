#!/bin/bash
# Sync all mod folders from dev folder to RimWorld Mods directory

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEV_DIR="$(dirname "$SCRIPT_DIR")"
MODS_DIR="$(dirname "$DEV_DIR")"

echo "Syncing mods to: $MODS_DIR"
echo ""

# Sync each mod folder
for MOD_DIR in "$DEV_DIR"/*/; do
    MOD_NAME="$(basename "$MOD_DIR")"
    
    # Skip non-mod directories
    if [[ "$MOD_NAME" == "scripts" ]] || [[ "$MOD_NAME" == ".vscode" ]] || [[ "$MOD_NAME" == ".git" ]]; then
        continue
    fi
    
    TARGET_DIR="$MODS_DIR/$MOD_NAME"
    
    echo "Syncing: $MOD_NAME"
    echo "  From: $MOD_DIR"
    echo "  To:   $TARGET_DIR"
    
    rsync -av --delete \
        --exclude='.git/' \
        --exclude='.vscode/' \
        --exclude='scripts/' \
        --exclude='Source/obj/' \
        --exclude='Source/bin/' \
        --exclude='*.code-workspace' \
        "$MOD_DIR" "$TARGET_DIR/"
    
    echo ""
done

echo "All mods synced!"
