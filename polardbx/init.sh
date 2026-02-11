python3 -m venv venv
source venv/bin/activate
.\venv\Scripts\activate
pip install --upgrade pip

pip3 install wheel -v
pip3 install "cython<3.0.0" pyyaml==5.4.1 --no-build-isolation -v
pip install pxd

pxd tryout
pxd list
pxd cleanup
pxd delete pxc-tryout