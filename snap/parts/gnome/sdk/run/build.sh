#!/bin/bash
set -euo pipefail
source /home/thang/data/Documents/ASP.NET-Project/snap/parts/gnome/sdk/run/environment.sh
set -x
make -j"12"
make -j"12" install DESTDIR="/home/thang/data/Documents/ASP.NET-Project/snap/parts/gnome/sdk/install"
