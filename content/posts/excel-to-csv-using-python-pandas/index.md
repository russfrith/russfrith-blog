---
title: "Excel to CSV Using Python Pandas"
date: 2022-07-20T18:20:31-04:00
draft: false
cover:
  image: fidias-cervantes-oHW-2VFNg-Q-unsplash.jpg
  alt: "Ball Python"
  caption: "Photo by [Fidias Cervantes](https://unsplash.com/@fidpad?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on [Unsplash](https://unsplash.com/s/photos/python?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText)"
tags: ["python", "pandas"]
categories: ["programming"]
---

## Introduction
Python Pandas is a powerful package used by data scientists to analyze data. My recent use case was far more pedestrian. I had one vendor that was providing a data file in Excel format, and another vendor that needed to consume that data in a completely different schema as a CSV file. 

As is often the case, the file I was receiving was in an odd format. I’m sure you can relate. The primary problem was that the columns were not all defined. They were unnamed and formatted for some previously defined form, such that the first column might be a person’s name, the second column might be the first line of the address, or a second person’s name.

Column 0   | Column 1  | Column 2   | Column 3
-----------|-----------|------------|-------------
Mary Smith | 1 Main St | Newark, NJ | 
John Doe   | Jane Doe  | 2 Main St  | Trenton, NJ
Hans Down  | 3 Main St | Ste 100    | Oakland, CA

The table above is a very simplified example of the problem, but we can use it to learn how Pandas can help us.

## Read Excel
Pandas can open files and load them into a dataframe. A dataframe is one of the primary datas structures of a Pandas project. In addition to reading and writing to Excel and CSV files, Pandas supports many other file formats, including JSON, XML, SQL, among other formats. We will open an XLSX files as shown below:

```python
import pandas as pd

df_input = pd.read_excel(open('input.xlsx', 'rb'), sheet_name='Sheet2')
```

My first thought was to find a library that parsed addresses. I found a promising option called usaddresses. It provided all of the features I needed,  but it did not appear to be in active development, and was only compatible with Python 2.7. Since I had a limited data set and could assume that addresses would start with a house number, and that names would not. Therefore, I was able to use numpy to identify names vs addresses.

## DataFrame.iloc
Since the file I was given did not include headings, and because of the problem described above, such headings would be meaningless, we must access columns by the integer index. Pandas provides the *DataFrame.loc* method to access labeled axes, and the DataFrame.iloc method to access columns based on the integer index. 

## numpy.select()
One option is to select what are names and what are addresses based on a list of conditions. Numpy provides the *numpy.select()* statement to achieve this. The *condlist* is a list of conditions that determine from which array in the choice list the output elements are taken. If multiple conditions are satisfied the first one in condlist is used. The *choicelist* is the list of arrays from which the output elements are taken. If all conditions evaluate to false, *default* value is returned.

```python
numpy.select(condlist, choicelist, default=0)
```

## Functions
Unlike many popular programming languages that use braces (or “curly brackets”), Python uses indentation to indicate a block of code. Therefore, a Python function looks like this:

```python
def my_function():
  print("Hello from a function")

my_function()
```

## Names & Addresses
Using what we have seen above, we can combine this to solve the name and address problem. I find this solution to be a bit brute force and ugly, and we will learn an alternative shortly, but my goal was to learn enough to solve the problem and move on. A deeper understanding of Python and these libraries may come later.


```python
import numpy as np

name_conditions = [
    ~df_input.iloc[:, 2].str[:1].str.isnumeric() & df_input.iloc[:, 3].str[:1].str.isnumeric(),
    ~df_input.iloc[:, 2].str[:1].str.isnumeric() & ~df_input.iloc[:, 3].str[:1].str.isnumeric() & df_input.iloc[:, 4].str[:1].str.isnumeric(),
    ~df_input.iloc[:, 2].str[:1].str.isnumeric() & ~df_input.iloc[:, 3].str[:1].str.isnumeric() & ~df_input.iloc[:, 4].str[:1].str.isnumeric()
]

names = [
    df_input.iloc[:, 2],
    df_input.iloc[:, 2] + '|' + df_input.iloc[:, 3],
    df_input.iloc[:, 2] + '|' + df_input.iloc[:, 3] + '|' + df_input.iloc[:, 4]
]

addresses = [
    df_input.iloc[:, 3] + ', ' + df_input.iloc[:, 4],
    df_input.iloc[:, 4] + ', ' + df_input.iloc[:, 5],
    df_input.iloc[:, 5] + ', ' + df_input.iloc[:, 6],
]

df_input['Full Name'] = np.select(name_conditions, names)
df_input['Address'] = np.select(name_conditions, addresses)

```

Once we make the necessary changes to our dataframe we can export it to a new file:

```python
df_input.to_csv('output.csv')
```