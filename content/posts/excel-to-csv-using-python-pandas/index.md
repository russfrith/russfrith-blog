---
title: "Excel to CSV Using Python Pandas"
date: 2022-05-31T18:20:31-04:00
draft: false
cover:
  image: fidias-cervantes-oHW-2VFNg-Q-unsplash.jpg
  alt: "Ball Python"
  caption: "Photo by [Fidias Cervantes](https://unsplash.com/@fidpad?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText) on [Unsplash](https://unsplash.com/s/photos/python?utm_source=unsplash&utm_medium=referral&utm_content=creditCopyText)"
tags: ["python", "pandas"]
categories: ["programming"]
---

Python Pandas is a powerful package used by data scientists to analyze data. My recent use case was far more pedestrian. I had one vendor that was providing a data file in Excel format, and another vendor that needed to consume that data in a completely different format as a CSV file. 

As is often the case, the file I was receiving was in an odd format. I’m sure you can relate. The primary problem was that the columns were not all defined. They were unnamed and formatted for some previously defined form, such that the first column might be a person’s name, the second column might be the first line of the address, or a second person’s name.

Column 0   | Column 1  | Column 2   | Column 3
-----------|-----------|------------|-------------
Mary Smith | 1 Main St | Newark, NJ | 
John Doe   | Jane Doe  | 2 Main St  | Trenton, NJ
Hans Down  | 3 Main St | Ste 100    | Oakland, CA

The table above is a very simplified example of the problem, but we can use it to learn how Pandas can help us.

I choose to create a Docker container for Python development so that I can specify the version and packages for each individual project. This avoids the version conflicts should another project require a specific version of Python or packages.

```dockerfile
FROM python:3

# Keeps Python from generating .pyc files in the container
ENV PYTHONDONTWRITEBYTECODE=1

# Turns off buffering for easier container logging
ENV PYTHONUNBUFFERED=1

# Install pip requirements
COPY requirements.txt ./
RUN python -m pip install --no-cache-dir -r requirements.txt

WORKDIR /app
COPY . /app

# Creates a non-root user with an explicit UID and permission to access the /app folder
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

CMD ["python", "./myapp.py"]
```

The dockerfile above refers to a requirements.txt file. While this may be overkill for this project, it makes it easier to add dependencies here as you need them, rather than via the pip command in dockerfile, which now can remain unchanged. If you require a new package, just add it to the list in the requirements file as shown below:

```
pandas>=1.4.0
numpy>=1.22.0
```

Pandas can open files and load them into a dataframe. A dataframe is one of the primary datas structures of a Pandas project. In addition to reading and writing to Excel and CSV files, Pandas supports many other file formats, including JSON, XML, SQL, among other formats. We will open an XLSX files as shown below:

```python
import pandas as pd

df_input = pd.read_excel(open('input.xlsx', 'rb'), sheet_name='Sheet2')
```

My first thought was to find a library that parsed addresses. I found a promising option called usaddresses. It provided all of the features I needed,  but it did not appear to be in active development. Since I had a limited data set and could assume that addresses would start with a house number, and that names would not. Therefore, I was able to use numpy to identify names vs addresses:

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