#!/bin/sh
SERVERIDS=$(vim-cmd vmsvc/getallvms | sed -e '1d' -e 's/ \[.*$//' | awk '$1 ~ /^[0-9]+$/ {print $1}')

for config in $SERVERIDS
do
  vim-cmd vmsvc/power.getstate $config | grep -i  "on" > /dev/null 2<&1
  STATUS=$?
  echo $STATUS 

  if [ $STATUS == 0 ]
  then
    echo "Shutdown VM $config"
    vim-cmd vmsvc/power.shutdown $config
  fi
done

esxcli system maintenanceMode set -e true -t 0 
esxcli system shutdown poweroff -d 10 -r "Shell initiated system shutdown By NG"
esxcli system maintenanceMode set -e false -t 0