function has($cmd) { !!(Get-Command $cmd -ErrorAction SilentlyContinue) }

# Install Python
if(!(has python)) {
    choco install python2
}

if(!(has python)) {
    throw "Failed to install python2"
}

# Install virtualenv
pip install virtualenv

# Make a virtualenv in .virtualenv
$VirtualEnvDir = Join-Path (Get-Location) ".virtualenv";

virtualenv $VirtualEnvDir

& "$VirtualEnvDir\Scripts\python" --version
& "$VirtualEnvDir\Scripts\pip" --version

# Install autobahn into the virtualenv
& "$VirtualEnvDir\Scripts\pip" install autobahntestsuite

# We're done. The travis config has already established the path to WSTest should be within the virtualenv.
Get-ChildItem .$VirtualEnvDir/bin
& "$VirtualEnvDir\Scripts\wstest" --version
