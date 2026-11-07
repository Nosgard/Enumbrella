# lib-gitleaks.sh: the gitleaks lookup and rules shared by pre-commit and pre-push.
# A hook loads it from its own folder with: . "$(dirname "$0")/lib-gitleaks.sh"
# It sets GITLEAKS and CONFIG and defines gitleaks_git and scan_push, or stops the hook.

hook=${0##*/}

GITLEAKS=$(command -v gitleaks 2>/dev/null)
WINGET="$LOCALAPPDATA/Microsoft/WinGet/Packages"
[ -z "$GITLEAKS" ] && for f in "$WINGET"/Gitleaks.Gitleaks_*/gitleaks.exe; do
  [ -x "$f" ] && GITLEAKS="$f"
done

if [ -z "$GITLEAKS" ]; then
  echo "$hook: gitleaks is not installed, refused." >&2
  echo "Install it with: winget install Gitleaks.Gitleaks / brew install gitleaks" >&2
  exit 1
fi

# Rules: $GITLEAKS_CONFIG, else .gitleaks.toml, else ~/.config/gitleaks/config.toml,
# else the gitleaks defaults.
CONFIG=
if [ -n "$GITLEAKS_CONFIG" ]; then
  CONFIG=$GITLEAKS_CONFIG
elif [ -f .gitleaks.toml ]; then
  CONFIG=.gitleaks.toml
elif [ -f "$HOME/.config/gitleaks/config.toml" ]; then
  CONFIG=$HOME/.config/gitleaks/config.toml
fi

# gitleaks_git <options>: scans the repository with the shared flags, exit code 3 on a finding.
gitleaks_git() {
  "$GITLEAKS" git "$@" --redact --no-banner --ignore-gitleaks-allow \
    --log-level warn ${CONFIG:+-c "$CONFIG"} --exit-code 3 .
}

# scan_push <remote> <local sha> <remote sha> <remote ref>: scans the commits of one pushed ref.
# A new branch, or a remote commit this repo does not have: all commits no tracking branch of
# the remote has. gitleaks skips a range git cannot resolve with exit code 0, so that refuses.
scan_push() {
  range="$3..$2"
  case "$3" in
    *[!0]*) git cat-file -e "$3^{commit}" 2>/dev/null || range="$2 --not --remotes=$1" ;;
    *) range="$2 --not --remotes=$1" ;;
  esac
  if ! git rev-list --quiet $range -- 2>/dev/null; then
    echo "$hook: git cannot resolve the commits for $4, push refused." >&2
    return 1
  fi
  if ! gitleaks_git --log-opts="$range"; then
    echo "$hook: secrets or personal data in the commits for $4, push refused." >&2
    return 1
  fi
}
