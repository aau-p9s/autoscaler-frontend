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

ps 56596;
while [ $? -eq 0 ];
do
  sleep 1;
  ps 56596 > /dev/null;
done;

for child in $(list_child_processes 56805);
do
  echo killing $child;
  kill -s KILL $child;
done;
rm /Users/jonathanwisborgfog/Documents/9.semester/projekt/autoscaler-frontend/autoscaler-frontend/autoscaler-frontend/bin/Debug/net7.0/0315a45b47bc445eb3449768f6808126.sh;
