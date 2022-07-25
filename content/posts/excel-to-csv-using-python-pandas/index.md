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

As is often the case, the file I was receiving was formed oddly. I’m sure you can relate. The primary problem was that the columns were not all defined. They were unnamed and formatted for some previously defined form, such that the first column might be a person’s name, the second column might be the first line of the address, or a second person’s name.

Column 0   | Column 1  | Column 2   | Column 3
-----------|-----------|------------|-------------
Mary Smith | 1 Main St | Newark, NJ | 
John Doe   | Jane Doe  | 2 Main St  | Oakland, CA

In the table above, column 0 is always a name. Column 1 may be a name, or an address, etc. The table above is a very simplified example of the problem, but we can use it to learn how Pandas can help us.

### Pandas Series
A *pandas.Series* is a one-dimensional array with axis labels. A Series is equatable to an Excel column. The object supports both integer and label-based indexing and provides methods for performing operations involving the index.

### Pandas DataFrame
A *pandas.DataFrame* is a two-dimensional data structure, like a two-dimensional array, or a table with rows and columns. You can think of it as a dictionary for Series objects. A dataframe is one of the primary datas structures of a Pandas project. 

### Read Excel
Pandas can open files and load them into a dataframe. In addition to reading and writing to Excel and CSV files, Pandas supports many other file formats, including JSON, XML, SQL, among others. We will open an XLSX files as shown below:

```python
import pandas as pd

df_input = pd.read_excel(open('input.xlsx', 'rb'), sheet_name='Sheet2')
```

### DataFrame iloc
Since the file I was given did not include headings, and because of the problem described above, such headings would be meaningless, we must access columns by the integer index. Pandas provides the *DataFrame.loc* method to access labeled axes, and the *DataFrame.iloc* method to access columns based on the integer index.

```python
mydict = [{'a': 1, 'b': 2, 'c': 3, 'd': 4},
          {'a': 100, 'b': 200, 'c': 300, 'd': 400},
          {'a': 1000, 'b': 2000, 'c': 3000, 'd': 4000 }]

df = pd.DataFrame(mydict)

print(df)
      a     b     c     d
0     1     2     3     4
1   100   200   300   400
2  1000  2000  3000  4000

print(df.iloc[0])
a    1
b    2
c    3
d    4

print(df.iloc[0, 1])
2

print(df.iloc[[0, 2], [1, 3]])
      b     d
0     2     4
2  2000  4000
```

### NumPy Select
One option is to select what are names and what are addresses based on a list of conditions. Numpy provides the *numpy.select()* statement to achieve this. The *condlist* is a list of conditions that determine from which array in the choice list the output elements are taken. If multiple conditions are satisfied the first one in *condlist* is used. The *choicelist* is the list of arrays from which the output elements are taken. If all conditions evaluate to false, *default* value is returned.

```python
numpy.select(condlist, choicelist, default=0)
```

### Functions
Unlike many popular programming languages that use braces (or “curly brackets”). Python uses indentation to indicate a block of code. Therefore, a Python function looks like this:

```python
def my_function():
  print("Hello from a function")

my_function()
```

## Names & Addresses
My first thought was to find a library that parsed addresses. I found a promising option called [usaddress](https://pypi.org/project/usaddress/). It provided all of the features I needed,  but it did not appear to be in active development, and was only compatible with Python 2.7. Since I had a limited data set and could assume that addresses would start with a house number, and that names would not, I was able to use NumPy to identify names vs addresses.

Using what we have seen above, we can combine this to solve the name and address problem. I find this solution to be a bit brute force and ugly, and we will learn an alternative shortly, but my goal was to learn enough to solve the problem and move on. A deeper understanding of Python and these libraries may come later.


```python
import numpy as np

def is_name(column):
    return ~df_input.iloc[:, column].str[:1].str.isnumeric()

name_conditions = [
    is_name(0) & ~is_name(1),
    is_name(0) & is_name(1) & ~is_name(2)
]

names = [
    df_input.iloc[:, 0],
    df_input.iloc[:, 0] + '|' + df_input.iloc[:, 1]
]

addresses = [
    df_input.iloc[:, 1] + ', ' + df_input.iloc[:, 2],
    df_input.iloc[:, 2] + ', ' + df_input.iloc[:, 3]
]

df_input['Full Name'] = np.select(name_conditions, names)
df_input['Address'] = np.select(name_conditions, addresses)

```

## Charges and Credits
In addition to the name and address challenge outlined above, I also needed to parse the transactions. In the file supplied by the source company, they had formatting built into the export file. Date, Description, Charge, Credit, etc. are all combined into one column, and we don’t know how many columns there will be. This was done so that the previous consumer could simply print the entire column, and it would be appropriately formatted. They were unwilling or unable to provide a cleaner export. Therefore, I needed to parse the column to separate the charges and the credits. 

Imagine the column is 80 characters wide. The first 10 characters include the Date. Characters 15 through 25 include a Description. Characters 35 through 45 include the charge if applicable. Characters 50 through 60 include the credit  if applicable. Finally, characters 70 through 80 include the subtotal. The columns could be parsed as follows:

## Wrapping Up
Python Pandas is great at manipulating data, but it can also be used to import., transform, and export data when the situation arises. I found it a relatively simple way to take a poorly planned dataset and manipulate it for use in another application. My use case combined with a smallish dataset made my brute-force approach usable. However, if you have a large dataset, or if you are using Pandas as intended, you should operate on a Series. I generally avoid hacks, but as much as it hurt me to share this, it did solve my problem.

### Write CSV
Once we make the necessary changes to our dataframe we can export it to a new file:

```python
df_input.to_csv('output.csv')
```

### PyInstaller
My script will need to be run regularly by someone else. Therefore, I need to create an executable version of my Python script that requires no dependencies. Two options are [auto-py-to-exe](https://pypi.org/project/auto-py-to-exe/) and [PyInstaller](https://pypi.org/project/pyinstaller/). I chose PyInstaller for the command line interface. 

```
pip install pyinstaller

pyinstaller --onefile name_of_script.py
```

Now anyone can run this executable on their Windows machine without the need for Python to be installed.
