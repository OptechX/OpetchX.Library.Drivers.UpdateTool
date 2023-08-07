#!/bin/bash

# Read the version number from version_info file
version=$(head -n 1 version_info)

# Escape the version string to be used in the sed command
escaped_version=$(printf '%s\n' "$version" | sed -e 's/[\/&]/\\&/g')

# Replace the version number in Program.cs with the value from version_info file
gsed -i "s/Console.WriteLine(\"Version: [0-9.]\+\");/Console.WriteLine(\"Version: $escaped_version\");/" ./OptechX.Library.Drivers.UpdateTool/Program.cs

# Run the commands in order
git add .
git commit -m "update to v$version"
git push
git tag -s "v$version" -m "Version $version"
git push --tags
