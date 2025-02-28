correct=true
ignore='$1'
root='$2'
config='$3'

echo "::debug::parse ignore"

mapfile -t -d ';' ignore_map < <(echo -n $ignore)
ignore_options=$(printf " ! -path %s " "${ignore_map[@]}")

echo "::debug::run find xamls"

mapfile -t xamls < <(find $root -name *.xaml $ignore_options )

for xaml in "${xamls[@]}"; do

echo "::debug::run xstyler passive - $xaml"

out=$(xstyler --config $config --passive --file $xaml)

err=$?
if [ $err = 0 ]; then
    echo "Checking: $xaml - PASS"
else
    echo "Checking: $xaml - FAIL"
    correct=false

    echo "::debug::run xstyler write-to-stdout"

    xaml_out=$(xstyler --config $config --write-to-stdout --loglevel None --file $xaml )
    xaml_src=$(cat $xaml)

    xaml_out="${xaml_out// /·}"
    xaml_out="${xaml_out//$'\t'/→}"

    xaml_src="${xaml_src// /·}"
    xaml_src="${xaml_src//$'\t'/→}"

    echo "::debug::run diff old-line-format"

    diff_lines=$(diff --unchanged-line-format="" --old-line-format="%dn;" --new-line-format="" --color <(echo "${xaml_src}") <(echo "${xaml_out}"))

    mapfile -t -d ';' lines < <(echo -n $diff_lines)

    for line in "${lines[@]}"; do
    echo "::error file=$xaml,line=$line,title=Invalid xaml style::"
    done


    echo "::debug::print group"

    echo "::group::File differences"
    diff_out=$(diff --color <(echo "${xaml_src}") <(echo "${xaml_out}"))
    echo -e "$diff_out"
    echo "endgroup::"
fi
done

if [ "$correct" = false ]; then
  exit 1
fi