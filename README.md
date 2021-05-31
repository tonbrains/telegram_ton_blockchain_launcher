# TON (Telegram Open Network) Blockchain Launcher
## _Powered by TON BRAINS tonbrains.com_

[![Build Status](https://travis-ci.org/joemccann/dillinger.svg?branch=master)](https://tonbrains.com)

One-Click TON blockchain launcher for R&D purposes. Indefinite validation cycles (No need to worry about elections). The Fastest TON Network configuration available.

## Please follow the Instruction to configure Nodes.

- Ubuntu 20.04 supported
- At least 2 nodes required to launch TON Blokchain
- use root 
- clon ton using github link "https://github.com/tonbrains/ton"
- cd "/root/ton"
- mkdir -p "/root/ton/build"
- cd "/root/ton/build"
- cmake .. -G "Ninja" -DCMAKE_BUILD_TYPE=Release -DPORTABLE=ON
- ninja

## TON Launcher Instructions.

Markdown is a lightweight markup language based on the formatting conventions
that people naturally use in email.

- clone repository https://github.com/tonbrains/telegram_open_network_launcher
- build project using .Net Core and Blazor Tools
- try use Windows 10 If you will difficulties with Linux
- Open Index.razor and add your nodes
- Launch Blazor App
- click button TON Rocket Launch. 
- Setup Can take few minutes.
- check network status by launching ton blockchain explorer:
"/root/ton/build/blockchain-explorer/blockchain-explorer" -p "/root/ton-keys/liteserver.pub" -C "/var/ton-work/etc/ton-global.config.json" -a 127.0.0.1:3031

## Key Features

- Custom Main Wallet
- New Zerostate (No Election mechanism)
- Cusomizable Network Configuration
- Only 2 node required
- for support please send a message to contact@tonbrains.com