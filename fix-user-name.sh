#!/bin/bash

# Fix User { Name = "..." } to use FirstName and LastName
find RedmineCLI.Tests -name "*.cs" -type f -exec sed -i \
  -e 's/User { Id = \([0-9]*\), Name = "\([^"]*\)" }/User { Id = \1, FirstName = "\2", LastName = "" }/g' \
  -e 's/User { Id = \([0-9]*\), Name = "\([^ ]*\) \([^"]*\)" }/User { Id = \1, FirstName = "\2", LastName = "\3" }/g' \
  {} +

# Special case for Japanese names (assuming given name first)
find RedmineCLI.Tests -name "*.cs" -type f -exec sed -i \
  -e 's/FirstName = "山田太郎", LastName = ""/FirstName = "太郎", LastName = "山田"/g' \
  -e 's/FirstName = "佐藤花子", LastName = ""/FirstName = "花子", LastName = "佐藤"/g' \
  -e 's/FirstName = "鈴木一郎", LastName = ""/FirstName = "一郎", LastName = "鈴木"/g' \
  {} +

echo "Fixed User.Name to use FirstName and LastName"