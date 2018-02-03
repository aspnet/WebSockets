function has($cmd) { !!(Get-Command $cmd -ErrorAction SilentlyContinue) }

# Install Python
if(!(has python)) {
    choco install python2
}

if(!(has python)) {
    throw "Failed to install python2"
}

# Print python version
python --version

# Install virtualenv
pip install virtualenv

# Make a virtualenv in .virtualenv
$VirtualEnvDir = Join-Path (Get-Location) ".virtualenv";

virtualenv $VirtualEnvDir

& "$VirtualEnvDir\bin\python" --version
& "$VirtualEnvDir\bin\pip" --version

# Install autobahn into the virtualenv
& "$VirtualEnvDir\bin\pip" install autobahntestsuite

# We're done. The travis config has already established the path to WSTest should be within the virtualenv.
Get-ChildItem .$VirtualEnvDir/bin
& "$VirtualEnvDir\bin\wstest" --version
