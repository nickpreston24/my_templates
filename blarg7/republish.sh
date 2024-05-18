echo "stopping running service ... "
sudo systemctl stop blarg7 # stop the blarg7 service to remove any file-locks
echo "service stopped."
echo "Republishing service..."
sudo dotnet publish -c Release -o /srv/blarg7 # release to your user directory
sudo cp .env /srv/blarg7/.env

echo "Updating systemctl ..."
sudo cp blarg7.service /etc/systemd/system/blarg7.service
sudo systemctl daemon-reload
sudo systemctl start blarg7  

echo "restarting service..."
sudo systemctl start blarg7 # start service
