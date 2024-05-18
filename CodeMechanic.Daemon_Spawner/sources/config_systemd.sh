# Copy service file to a System location
sudo cp your_daemon.service /lib/systemd/system

# Reload SystemD and enable the service, so it will restart on reboots
sudo systemctl daemon-reload 
sudo systemctl enable your_daemon

# Start service
sudo systemctl start your_daemon 

# View service status
systemctl status your_daemon
