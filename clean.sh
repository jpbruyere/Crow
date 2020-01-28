#!/bin/bash
rm -fr build && find . -iname bin -o -iname obj -o -iname packages -o -iname build | xargs rm -rf
