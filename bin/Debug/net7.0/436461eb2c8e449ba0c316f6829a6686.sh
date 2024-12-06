function list_child_processes () {
    local ppid=$1;
    local current_children=$(pgrep -P $ppid);
    local local_child;
    if [ $? -eq 0 ];
    then
        for current_child in $current_children
        do
          local_child=$current_child;
          list_child_processes $local_child;
          echo $local_child;
        done;
    else
      return 0;
    fi;
}

ps 17554;
while [ $? -eq 0 ];
do
  sleep 1;
  ps 17554 > /dev/null;
done;

for child in $(list_child_processes 17569);
do
  echo killing $child;
  kill -s KILL $child;
done;
rm /Users/jonathanwisborgfog/Documents/9.semester/projekt/autoscaler-frontend/bin/Debug/net7.0/436461eb2c8e449ba0c316f6829a6686.sh;
