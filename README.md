BBP Pi Calculator
===============

Preliminary C#.NET project to generate the numbers of pi using the BBP formula which is grid computing friendly.

BBP Pi Calculator

Updated in 2021:
- Extended to include digit generator, tests simplified and expanded.
- Dotnet 6
- currently VS2022/Rider EAP required.
- 'miner' branch has further additions with a FasterKV based miner, including a Pi byte buffer and a worker/miner for filling the Pi FasterKV

2021.10.02
Jessica Mulein; jessicamulein.com, github.com/JessicaMulein

Originally by:
2014.12.14
Payson Welch; paysonwelch.com, freeideas.org, github.com/paysonwelch
paysonwelch@gmail.com

Abstract
--------------------------
This application is a proof of concept to demonstrate the capability of the 
Bailey–Borwein–Plouffe formula which can be used to solve for [n]th binary 
digit in base16 of pi.
http://en.wikipedia.org/wiki/Bailey%E2%80%93Borwein%E2%80%93Plouffe_formula

Executive Summary
--------------------------------------------------------
Originally I was considering using the BBP formula to create a new Proof of
Work (POW) system for crypto currencies. With the advent of Proof of Stake
however and other anonymizing features of currencies the BBP-Pi POW system
would not be enough in itself to create a highly successful crypto currency.

A BBP-Pi POW currency would be more of a novelty of interest to reserachers
mostly. There is also PrimeCoin which uses a POW system for hunting chains
of prime numbers. So although this is an interesting idea it needs more
thought before simply creating a POW based on BBP.

This applicaiton has been structured in such a way that it could easily be
modified for grid computing. This application currently will not focus on 
grid payload distribution, but will focus on breaking work items
into small specific units which can be easily inserted into a grid pipeline
in the future.
http://en.wikipedia.org/wiki/Primecoin
 
Bailey–Borwein–Plouffe 
--------------------------------------------------------
The BBP formula was accidentally discovered in 1995 but it changed the 
landscape of our understanding of how to calculate certain numbers such
as Pi and e. It uses a series of summations to calculate a hexidecimal 
stream of the floating point portion of Pi.

Compatibility and Crypto Currency Considerations
--------------------------------------------------------
This applicaiton is not compatible with large numbers, so be cautious about
overflow.

The GMP library has interesting number generators which could also be 
introduced for POW systems.
https://gmplib.org/pi-with-gmp.html

References: 
BBP Algorith for Pi, The (Whitepaper) http://crd-legacy.lbl.gov/~dhbailey/dhbpapers/bbp-alg.pdf
Pi in hexidecimal: http://calccrypto.wikidot.com/math:pi-hex

Parallelization, Test system: 2x Xeon E5520 CPUs, 24GB RAM
- Un-Parallelized runtime for 1000 slices (10000 digits) is 11.5 seconds.
- Parallelized runtime, 1.25 seconds for 1000 slices

Closing thought - The BBP algorithm is a prime candidate for conversion to 
C++ in order to use Accelerated Massive Parallelism (AMP) to facilitate 
GPU calculation.
