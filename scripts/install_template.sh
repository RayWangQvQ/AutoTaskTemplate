#!/bin/bash

cd ../framework

echo "Cleaning"
find . -type d -name "obj" -exec sh -c 'rm -rf "{}" && echo Delete {}' \;
find . -type d -name "bin" -exec sh -c 'rm -rf "{}" && echo Delete {}' \;
find . -type d -name ".vs" -exec sh -c 'rm -rf "{}" && echo Delete {}' \;
echo "Deletion complete."

echo "Installing"
dotnet new install ./ --force
read -p "Press [Enter] key to continue..."