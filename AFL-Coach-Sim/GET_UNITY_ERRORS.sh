#!/bin/bash
# Script to check for common Unity compilation issues

echo "=== Checking Unity Project for Compilation Issues ==="
echo ""

# Check for missing .meta files
echo "Checking for missing .meta files..."
find Assets/Scripts/Managers/MatchFlowManager.cs Assets/Scripts/UI/Match*.cs Assets/Scripts/Systems/SeasonProgressionController.cs Assets/Scripts/Systems/TeamSelection/*.cs Assets/Scripts/Utilities/*.cs -type f 2>/dev/null | while read file; do
    if [ ! -f "${file}.meta" ]; then
        echo "❌ Missing .meta file for: $file"
    fi
done

echo ""
echo "=== If Unity is showing errors, please copy them here ==="
echo "Open Unity → Console window → Right-click errors → Copy"
echo ""
echo "Common error types to look for:"
echo "1. 'The type or namespace name X could not be found'"
echo "2. 'CS0246' errors"
echo "3. 'does not contain a definition for'"
echo "4. Assembly reference errors"
