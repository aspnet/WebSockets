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
virtualenv .virtualenv

.virtualenv/bin/python --version
.virtualenv/bin/pip --version

# Install autobahn into the virtualenv
.virtualenv/bin/pip install autobahntestsuite

# We're done. The travis config has already established the path to WSTest should be within the virtualenv.
Get-ChildItem .virtualenv/bin
.virtualenv/bin/wstest --version
