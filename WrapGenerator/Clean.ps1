$originalDir = $pwd
cd $PSScriptRoot

dir -r obj -Directory | rm -r
dir -r bin -Directory | rm -r
dir -r gen -Directory | rm -r

cd $originalDir