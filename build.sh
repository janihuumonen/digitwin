#!/bin/sh

files="ws-srv.ts ws-prod.sh ws-cons.ts WebSocketClient.cs main.py"

if [ ! -e auth.cfg ]; then
	cat <<- EOF
		Created auth.cfg.
		Edit it and replace placeholders with your URLs and auth keys!
		Then run $0 again.
	EOF
	cat > auth.cfg <<- EOF
		<WS_API_ENDPOINT> wss://your_url_here
		<POST_API_ENDPOINT> https://your_url_here
		<CONSUMER_TOKEN> your_consumer_shared_secret_here
		<PRODUCER_TOKEN> your_producer_shared_secret_here
	EOF
	exit 1
fi

# replace connection placeholders with real values from auth.cfg
pl='
BEGIN {
  my $mapfile = shift @ARGV;
  open my $fh, "<", $mapfile or die "Cannot open $mapfile: $!";
  while (<$fh>) { chomp; my ($a,$b) = split; push @map, [$a,$b]; }
}
for my $m (@map) { s/\Q$m->[0]\E/$m->[1]/g; }'

mkdir -p build
for f in $files; do
	perl -pe "$pl" auth.cfg src/$f > build/$f
done
