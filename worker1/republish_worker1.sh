echo "stopping running service ... "
sudo systemctl stop worker1 # stop the worker1 service to remove any file-locks
echo "service stopped."
echo "Republishing service..."
sudo dotnet publish -c Release -o /srv/worker1 # release to your user directory
sudo cp .env /srv/worker1/.env

echo "Updating systemctl ..."
sudo cp worker1.service /etc/systemd/system/worker1.service
sudo systemctl daemon-reload
sudo systemctl start worker1  

echo "restarting service..."
sudo systemctl start worker1 # start service
