#!/bin/bash

SCRIPTDIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
EXECUTABLE=${SCRIPTDIR}/TallyClient;
USERNAME=`id -un`
GROUPNAME=`id -gn`

echo "test"

echo $SCRIPTDIR
echo $EXECUTABLE

chmod +x $EXECUTABLE

cat << EOF | sudo tee /etc/systemd/system/tally-client.service
[Unit]
Description="Tally-Client from  https://github.com/Dalesjo/Atem-Tally"

[Service]
User=${USERNAME}
Group=${GROUPNAME}
Type=notify
WorkingDirectory=${SCRIPTDIR}
ExecStart=${EXECUTABLE}

[Install]
WantedBy=multi-user.target

EOF

sudo systemctl daemon-reload
sudo systemctl enable tally-client
sudo systemctl start tally-client
sudo systemctl status tally-client
