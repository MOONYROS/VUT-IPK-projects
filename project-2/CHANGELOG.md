# IPK Project 2 (IOTA variant) CHANGELOG

Author: Ondrej Lukasek (xlukas15)

## Case-sensitive

Since there was not explicitly said (in the task), whether the instructions parsing is case-sensitive or not, I have decided that all the instructions have to be written in capital letter, thus case-sensitive.

## Using double in expression evaluation

Internal expression evaluation is calculated using `double` variable type, but final result is checked to have only one digit as in the assignment. That means that intermediate results can have **more than one digit** or even be a fraction, but input numbers and end result are one digit only.
