#!/bin/bash
rm -rf cap/config/*
cp cap-template/config/smartreader.json cap-template/config/smartreader.json
chmod 755 cap/start
chmod 755 cap/SmartReaderStandalone
chmod 755 cap/*.so
chmod 777 cap/config
chmod 777 cap/config/*.ini
chmod 777 cap/config/*.json
chmod 777 cap/config/*.config
chmod 755 cap/*.a
chmod 755 cap/*.dll
chmod 755 cap/*.json
dos2unix cap/*.json
dos2unix cap/*.config
dos2unix cap/config/*.config
dos2unix cap/*.ini
dos2unix cap/config/*.ini
dos2unix cap/config/*.json
../cap_gen.sh -d cap_description.in -o smartreader_cap.upgx
