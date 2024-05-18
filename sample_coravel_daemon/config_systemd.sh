# Copy service file to a System location
sudo cp sample_coravel_daemon.service /lib/systemd/system

# Reload SystemD and enable the service, so it will restart on reboots
sudo systemctl daemon-reload 
sudo systemctl enable sample_coravel_daemon

# Start service
sudo systemctl start sample_coravel_daemon 

# View service status
systemctl status sample_coravel_daemon
