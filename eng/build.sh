# Resolve any symlinks in the given path.
function ResolvePath {
  local path=$1

  while [[ -h $path ]]; do
    local dir="$( cd -P "$( dirname "$path" )" && pwd )"
    path="$(readlink "$path")"

    # if $path was a relative symlink, we need to resolve it relative to the path where the
    # symlink file was located
    [[ $path != /* ]] && path="$dir/$path"
  done

  # return value
  _ResolvePath="$path"
}

blue=`tput setaf 4`
green=`tput setaf 2`
reset=`tput sgr0`
echo "${green}Build in $1 configuration${reset}"

# Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
export DOTNET_MULTILEVEL_LOOKUP=0

# Disable first run since we want to control all package sources
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Disable telemetry on CI
export DOTNET_CLI_TELEMETRY_OPTOUT=1
  

ResolvePath "${BASH_SOURCE[0]}"
eng_root=`dirname "$_ResolvePath"`
repo_root=`cd -P "$eng_root/../" && pwd`
repo_tools=$repo_root/.tools
dotnet_root=$repo_tools/dotnet
dotnetcli=$dotnet_root/dotnet

# Add dotnet to PATH. This prevents any bare invocation of dotnet in custom
# build steps from using anything other than what we've downloaded.
export PATH="$dotnet_root:$PATH"

install_script="$repo_tools/dotnet-install.sh"
install_script_url="https://dot.net/v1/dotnet-install.sh"

echo "Downloading '$install_script_url'"

 # Use curl if available, otherwise use wget
if command -v curl > /dev/null; then
  curl "$install_script_url" -sSL --retry 10 --create-dirs -o "$install_script"
else
  wget -q -O "$install_script" "$install_script_url"
fi

bash "$install_script" --version 2.2.203 --install-dir "$dotnet_root" || {
  local exit_code=$?
  echo "Failed to install dotnet SDK (exit code '$exit_code')." >&2
  ExitWithExitCode $exit_code
}

# Add dotnet to PATH. This prevents any bare invocation of dotnet in custom
# build steps from using anything other than what we've downloaded.
export PATH="$dotnet_root:$PATH"

echo "${blue}Build with driver${reset}"
echo 

$dotnetcli --info

$dotnetcli msbuild $eng_root/build.proj /p:Configuration=$1