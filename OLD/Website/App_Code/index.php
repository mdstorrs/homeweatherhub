<?php
$date = date_create();
$handle = fopen("./ws/ws" . date_timestamp_get($date), "w");
$result = print_r($_POST, true);
print_r(error_get_last());
fwrite($handle, $result);
fclose($handle);
?>