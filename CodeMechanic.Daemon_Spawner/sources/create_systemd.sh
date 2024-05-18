cat > your_daemon.service <<EOF
[Unit]
Description=Demo service
After=network.target

[Service]
ExecStart=/usr/bin/dotnet $(pwd)/bin/your_daemon.dll 5000
Restart=on-failure

[Install]
WantedBy=multi-user.target
EOF
