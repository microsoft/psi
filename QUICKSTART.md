# `QUICKSTART.md`

# `LINUX` - build `\psi` on Ubuntu/Debian

Build `\psi` on Ubuntu `LINUX`; only tested on `amd64` architecture.

## Step 1
Install `.Net Core` version [3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

### Both **Ubuntu 18.04** _and_ **Ubuntu 20.04**

```
sudo apt update -qq -y
sudo apt install -qq -y apt-transport-https
wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg --purge packages-microsoft-prod 
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update -qq -y
sudo apt install -y dotnet-sdk-3.1 dotnet-runtime-3.1
```

## Step 2 
Install [`mono-devel`](https://www.mono-project.com/download/stable/); use appropriate commands based on Ubuntu version, see below:

### **Ubuntu 18.04**

```
sudo apt install gnupg ca-certificates
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
sudo apt update -qq -y
sudo apt install -qq -y mono-devel
```

### **Ubuntu 20.04**

```
sudo apt install gnupg ca-certificates
sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
sudo apt update -qq -y
sudo apt install -qq -y mono-devel
```

## Step 3
Build `\psi`

```
./build.sh
```
