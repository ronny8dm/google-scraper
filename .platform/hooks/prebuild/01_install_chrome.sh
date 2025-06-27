#!/bin/bash

echo "Installing Google Chrome..."

# Update package list
yum update -y

# Install Chrome dependencies
yum install -y \
    wget \
    unzip \
    libX11 \
    libXcomposite \
    libXcursor \
    libXdamage \
    libXext \
    libXi \
    libXrandr \
    libXrender \
    libXss \
    libXtst \
    libxkbcommon \
    libdrm \
    libxshmfence \
    libnss3 \
    libgconf-2-4 \
    libXScrnSaver \
    libxss1 \
    libasound2

# Download and install Chrome
wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | rpm --import -
echo "[google-chrome]
name=google-chrome
baseurl=http://dl.google.com/linux/chrome/rpm/stable/x86_64
enabled=1
gpgcheck=1
gpgkey=https://dl.google.com/linux/linux_signing_key.pub" > /etc/yum.repos.d/google-chrome.repo

yum install -y google-chrome-stable

echo "Chrome installation completed"