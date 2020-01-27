#!/bin/bash
rm -fr build && find . -iname bin -o -iname obj -o -iname packages | xargs rm -rf
