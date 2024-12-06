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

ps 83372;
while [ $? -eq 0 ];
do
  sleep 1;
  ps 83372 > /dev/null;
done;

for child in $(list_child_processes 83386);
do
  echo killing $child;
  kill -s KILL $child;
done;
rm /Users/jonathanwisborgfog/Documents/9.semester/projekt/autoscaler-frontend/autoscaler-frontend/autoscaler-frontend/bin/Debug/net7.0/50d57a4bb2a340a2bab6d289f431a098.sh;
