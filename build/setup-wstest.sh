#!/usr/bin/env bash

type -p python
python --version

# Install local virtualenv
mkdir .python
cd .python
curl -OL https://pypi.python.org/packages/d4/0c/9840c08189e030873387a73b90ada981885010dd9aea134d6de30cd24cb8/virtualenv-15.1.0.tar.gz
tar xf virtualenv-15.1.0.tar.gz
cd ..

# Make a virtualenv
python ./.python/virtualenv-15.1.0/virtualenv.py .virtualenv

# We have to update pip on macOS to ensure we have one that supports TLS > 1.2.
# We do the update INSIDE the virtualenv (for permission reasons).
if [ "$TRAVIS_OS_NAME" == "osx" ]; then
    curl https://bootstrap.pypa.io/get-pip.py | .virtualenv/bin/python
fi

.virtualenv/bin/python --version
.virtualenv/bin/pip --version

# Install autobahn into the virtualenv
.virtualenv/bin/pip install autobahntestsuite

# We're done. The travis config has already established the path to WSTest should be within the virtualenv.
ls -l .virtualenv/bin
.virtualenv/bin/wstest --version