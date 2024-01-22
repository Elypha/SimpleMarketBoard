# SimpleMarketBoard

![2024-01-23_07-51-49_150](https://github.com/Elypha/SimpleMarketBoard/assets/30290883/1be58c0b-9e72-4c3f-90a3-66d564ea6ca1)

## Introduction

A simple but ultimate market board plugin caters for me at present, and potential power users who have the same requirements as me.

### Features

- **Compact UI design**, that won't eat out your screen even on your grandma's 800x600 CRT
- **Search history**, and with cache for review when you work with multi-items and a button to delete an entry
- **Flexible Keybindings**, so long as you can press it on your keyboard
- **More data, more money**
    - Universalis Last Updated Time (hrs)
      - You'll find this tiny table sitting in the bottom right corner.
      - It shows you how outdated the data from each world. You are encouraged to checkout yourself and the public data gets updated
    - Universalis Velocity
      - It is right above `Universalis Last Updated Time`
      - The velocity exhibits a monotonically increasing correlation with the aggregate volume of goods sold within a specified time interval
    - (TBD) Data from other DC
- **An HQ filter**, to display HQ only if you wish
- **Colour highlight**, so you tell HQ items visually instead of textually

## Whydunit

Popular plugins are great indeed but power users do have very distinct requirements that even if being coded into functionality, seamlessly incorporating them while respecting the original design of the project proves to be a real challenge. This will also lead to a plugin ending up packed with a ton of features that require tons of time to maintain. It's also against my personal work philosophy which revolves around constructing a toolkit with tools that excel in single functions, as opposed to settling for compromises among multifunctional tools.

You can't really expect volunteer developers to accommodate requirements they don't use or that might clash with their existing setups. That's why I took the initiative and wrote my own solution.

I'm not a C# developer and I have only confirmed it works on my setup.

## Acknowledgement

This project is heavily inspired by [MarketBoardPlugin](https://github.com/fmauNeko/MarketBoardPlugin), [PriceCheck](https://github.com/kalilistic/PriceCheck) and [SimpleTweaksPlugin](https://github.com/ottercorp/SimpleTweaksPlugin). Their developers had already put a considerable amount of effort into it, and I, who is not a C# developer, took advantage of several copy-pasting from the codebase of these projects. So, in a way, this project is built on the groundwork laid by its predecessors and I need to acknowledge this.
