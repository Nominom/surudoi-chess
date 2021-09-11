# Surudoi chess - A work in progress chess engine written in C#

鋭い (surudoi), meaning sharp or pointed, is a bitboard-based chess engine written in C#. 
The aim is to be a highly performant engine that can rival the search speeds of many C++ engines.

The current state of this project is work in progress, but the engine can already play fairly well using the UCI-protocol. 
Current ELO is in the high 1800s, rivaling engines like Shallow Blue 2.0 and Pigeon 1.5.1. 
The playing strength will increase dramatically once a few bugs are fixed and evaluation is tuned.

A huge thanks to all the people contributing in [Chess Programming wiki](https://www.chessprogramming.org/Main_Page) 
for sharing their knowledge, reference code and tips on how to build a chess engine. This, as well as thousands of other hobbyist engines
would not have been possible without the information made available by them.

## Search

Search is based on a principal variation search (PVS) with a zero-window scout search. This search works best with good move ordering, 
which in surudoi is based on MVV-LVA with extra information derived from attack- and defense bitboards.

## Evaluation

Coming soon...

## Test results

Coming soon...
