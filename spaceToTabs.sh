#!/bin/bash
sed -i.bak ':a;s/^\(\t*\) \{4\}/\1\t/;/^\t* \{4\}/ba' $1
rm $1.bak
