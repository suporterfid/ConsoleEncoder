#!/bin/bash

# Function to load the configuration from config.sh
load_config() {
  source /customer/config.sh
}

load_upgrade() {
  source /customer/upgrade_config.sh
}

# Initial load of the configuration
load_config

try_counter=0

# Loop
while true; do
    # Make use of the variables from the configuration
    echo "URL: $url_upgrade"
    #echo "Username: $username"
    #echo "Password: $password"

    # Some operations inside the loop

    # Reload the configuration at the end of each loop iteration
    load_config

    if [ -f /customer/upgrading ] ; then
        # Make the HTTP GET request using curl with SSL certificate validation and basic authentication
        response=$(curl -s -k -u "$username:$password" "$url_upgrade")

        # Use jq to extract the value of the "status" property
        status=$(echo "$response" | jq -r '.status')

        # Check the value of the "status" property and print the result
        if [ "$status" = "successful" ]; then
            echo "upgrade succeeded."
            /usr/bin/logger -p user.notice "upgrade succeeded."
        #response=$(curl -s -k -u "$username:$password" "$url_reboot")
            curl -X POST -H "Content-Type: application/json" -d '{}' -k -u "$username:$password" "$url_reboot"
        elif [ "$status" = "verifying" ]; then
            echo "verifying upgrade."    
            /usr/bin/logger -p user.notice "verifying upgrade."
        elif [ "$status" = "ready" ]; then
            echo "ready state detected, rebooting."    
            /usr/bin/logger -p user.notice "ready state detected, rebooting."
            curl -X POST -H "Content-Type: application/json" -d '{}' -k -u "$username:$password" "$url_reboot"
        elif [ "$status" = "installing" ]; then
            echo "installing upgrade."
            /usr/bin/logger -p user.notice "installing upgrade."
                            (( count = count + 1 ))
        elif [ "$status" = "failed" ]; then
            try_counter=$((try_counter+1))
            echo "upgrade failed."
            /usr/bin/logger -p user.notice "upgrade failed."
            echo "$try_counter"
            /usr/bin/logger -p user.notice "tried $try_counter times"
            if [ "$try_counter" -gt 4 ]; then
              echo "The script has tried more than $try_counter times, rebooting."
              /usr/bin/logger -p user.notice "The script has tried more than $try_counter times, rebooting."
              curl -X POST -H "Content-Type: application/json" -d '{}' -k -u "$username:$password" "$url_reboot"
            else
                echo "config image upgrade $url_download"
                /usr/bin/logger -p user.notice "config image upgrade $url_download"                
                /opt/ys/rshell -c "config image upgrade $url_download"
                echo "retrying..."
                /usr/bin/logger -p user.notice "retrying..."
            fi
        else
            echo "Unknown status: $status, rebooting."
            /usr/bin/logger -p user.notice "Unknown status: $status, rebooting."
            curl -X POST -H "Content-Type: application/json" -d '{}' -k -u "$username:$password" "$url_reboot"
        fi
    fi
   

    # Sleep for a while before the next iteration
    sleep 2
done
