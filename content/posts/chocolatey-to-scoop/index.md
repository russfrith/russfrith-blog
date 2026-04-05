---
title: "Scoop Over Chocolatey: A Windows Package Manager Transition Guide"
description: "Finding the right Windows package manager for your needs."
date: 2024-03-04T15:08:01-04:00
draft: false
showToc: true
cover:
  image: Russell_A_realistic_scene_showcasing_a_tub_of_rich_creamy_choco_5fb57ffa-96ce-4b08-8edc-cb882e675499.jpg
  alt: "Scoops of chocolate ice cream in a bowl"
  caption: "Image created by author using [Midjourney](https://www.midjourney.com/)"
tags: ["windows", "packagemanagers", "chocolatey", "scoop"]
categories: ["programming"]
---

## Introduction
For the last decade, I have been using Chocolatey as my package manager on Windows. I recently needed to install software on a new machine, and decided that now was an ideal time to revisit some popular package managers and perhaps make a switch.

One of my favorite benefits of package managers is the ability to install software in a consistent way across a variety of different applications. Having a command line tool with remote repositories of current versions of software makes it easy to keep applications up to date. Finally, it makes them easy to remove in a unified way should they no longer be needed. When setting up a new machine, the ability to have a script that installs most if not all the applications that I will need on that machine using such a tool is also highly beneficial.

## Package Managers
A package manager is a tool or system that automates the processes of installing, upgrading, configuring, and using software, primarily aimed at developer tools. Developers specify their required tools, and the package manager installs and configures them based on these specifications. This not only saves time in setting up a development environment, but also ensures consistency in the versions of packages installed. Let's explore some of the most popular package managers and examine their respective strengths and weaknesses.

### winget
Windows Package Manager winget command-line tool is available on current versions of Windows as a part of the App Installer, launched by Microsoft to simplify application management on Windows systems. Announced in May 2020 at Microsoft Build, this command-line tool allows users to discover, install, upgrade, remove, and configure applications, filling a gap in Windows' developer tools by providing functionality similar to package managers in Linux or macOS. Its introduction marks a significant step by Microsoft to enhance the Windows developer experience.

winget distinguishes itself from other popular Windows package managers in several ways:

1. **Integration with Windows**: As a Microsoft product, winget is closely integrated with the Windows ecosystem. This integration offers potential for more seamless updates and compatibility with Windows-specific features and updates.

2. **Official Microsoft Support**: winget is developed and supported by Microsoft, which may provide a sense of security and stability for users and organizations, particularly in terms of long-term support and integration with other Microsoft products.

3. **Open Source and Community Contributions**: Unlike Ninite or the discontinued AppGet, winget is open source, allowing the community to contribute to its development, suggest features, and improve the tool over time.

4. **Manifest-Based System**: winget uses a manifest system for package definitions, which can be more standardized and consistent compared to some methods used in other package managers like Chocolatey or Scoop.

5. **Native Command-Line Experience**: winget offers a native command-line experience that is consistent with other Windows command-line tools, which might be more familiar to Windows users than the interfaces provided by Chocolatey or Scoop.

6. **No Need for Third-Party Repository**: While Chocolatey and Scoop rely heavily on community-maintained repositories, winget can pull software directly from the Microsoft Store in addition to its community repository, potentially offering more official and trusted sources for software.

7. **Scripted Installations and Batch Operations**: winget allows for more complex scripted installations and batch operations, similar to Chocolatey and Scoop, but with the potential for deeper integration with Windows-specific features.

8. **Community-Driven Repositories**: Like Scoop and Chocolatey, winget allows for community-driven repositories, but being a Microsoft product, it potentially has a broader reach and might garner a more extensive repository of packages.

In summary, winget combines the familiarity and integration of a Microsoft product with the flexibility and community-driven approach of open-source package managers like Chocolatey and Scoop, while also providing a native and streamlined command-line interface for Windows users.

### Chocolatey
Chocolatey is distinguished by its extensive repository encompassing a wide array of both open-source and proprietary software. It excels in offering advanced features like custom package creation, deep integration with Windows and PowerShell for enhanced scripting, and robust version control for precise software management. Particularly appealing to system administrators and power users, Chocolatey supports integration with configuration management tools like Puppet, Chef, and Ansible, facilitating automated, consistent configurations across multiple machines. Its command-line interface is comprehensive and user-friendly, catering to both seasoned scripters and those new to command-line operations. The community-driven approach in maintaining and updating its packages further adds to its appeal, ensuring a broad software selection and timely updates.

Chocolatey distinguishes itself from other Windows package managers through several unique features and capabilities:

1. **Extensive Software Repository**: Chocolatey offers a vast and diverse repository of both open-source and proprietary software, surpassing the variety found in other managers like winget, Scoop, or Ninite.

2. **Package Creation and Customization**: Unlike Ninite or AppGet, Chocolatey allows users and organizations to create and host their own packages. This feature is particularly beneficial for businesses that need to manage internal software distributions or specific software configurations.

3. **Integration with Windows Features and PowerShell**: Chocolatey's deep integration with Windows features, especially PowerShell, stands out compared to alternatives. This integration allows for more advanced scripting and automation, which is particularly appealing for system administrators and power users.

4. **Version Control and Software Management**: Chocolatey excels in its ability to install specific versions of software and manage upgrades and downgrades, offering more control compared to winget and Ninite. This is crucial in environments where specific software versions are required for compatibility or testing purposes.

5. **Community-Driven Package Maintenance**: The Chocolatey community plays a significant role in maintaining and updating packages. This community-driven approach often leads to quicker updates and a broader range of software than what might be found in more centrally managed repositories like winget or Ninite.

6. **Compatibility with Configuration Management Tools**: Chocolatey uniquely integrates with configuration management tools like Puppet, Chef, and Ansible. This makes it a more versatile choice for enterprise environments where consistent and automated configuration across multiple Windows machines is essential.

7. **Comprehensive CLI Support**: While Scoop and winget also offer command line interfaces, Chocolatey's CLI is more robust and feature-rich, providing a level of control and flexibility that is particularly beneficial in automated scripts and batch operations.

8. **Security and Compliance Features**: For enterprise users, Chocolatey offers additional security and compliance features, like package internalization and private repositories, which are not as prominent in winget, Scoop, or Ninite.

In summary, Chocolatey's unique combination of an extensive software repository, package creation and customization, deep Windows integration, advanced version control, community support, enterprise features, and powerful CLI distinguishes it from other Windows package managers like winget, Scoop, and Ninite.

### Scoop
Scoop is a command-line package manager for Windows, known for its simplicity and focus on user convenience. It stands out for its straightforward setup and zero-configuration philosophy, appealing especially to developers and those who prefer a minimalistic approach. Scoop installs programs to the user's home directory, avoiding system-wide changes and making it non-intrusive compared to traditional Windows installers. This approach also facilitates easy backup and sync of installed applications. It primarily focuses on open-source and developer tools, offering a curated selection that simplifies the installation and update process for commonly used utilities and applications. Scoop's repository is community-maintained, ensuring a relevant and up-to-date software selection. Its ease of use, coupled with the ability to handle dependencies elegantly, makes it a favored choice for users seeking a lightweight and efficient package management solution on Windows.

Scoop has several unique features:

1. **Focus on Developer Tools**: Scoop is primarily focused on making it easy for developers to install tools they need. It's optimized for command-line users and simplifies the process of managing development tools.

2. **User Space Installation**: Unlike some other package managers, Scoop installs programs in the user's space, not system-wide. This means no administrator privileges are needed for installation, and it doesn't add to the system PATH by default, reducing potential system conflicts.

3. **Portable Applications**: Scoop is designed with a preference for portable applications. This means that applications are less likely to interfere with the rest of the system, as they are self-contained.

4. **Simplified App Management**: Scoop provides a simple command line interface for installing applications, without the need for a GUI. It also allows for easy version management and updating of installed programs.

5. **Git-Powered**: Scoop uses Git to manage its buckets (collections of applications), making it easy for users to add or create their own buckets. This contrasts with the centralized repositories of some other package managers.

6. **Manifest-Driven**: In Scoop, each application has a manifest that describes how it should be installed. This makes it easier to add new applications to Scoop compared to some other package managers that may require more complex packaging processes.

7. **Focus on Avoiding Bloatware**: Scoop tends to avoid applications that come bundled with unwanted extras, a practice sometimes seen in other free software distributions.

8. **Customizability and Scripting**: Because of its command-line nature and use of PowerShell, Scoop is highly customizable and can be integrated into scripts for automation purposes.

In contrast:

- **winget** is Microsoft's own package manager, integrated with Windows and focusing more broadly on all types of software.
- **Chocolatey** is more feature-rich and caters to both end-users and enterprise needs with a focus on comprehensive package management solutions, including GUI support.
- **Ninite** is known for its simplicity and is often used for quick, bulk installations of popular applications, usually for setting up new systems.

Each of these package managers serves different user needs and preferences, with Scoop being particularly favorable for developers and command-line enthusiasts who prefer a minimalistic and scriptable approach.

### Ninite
Ninite is a streamlined package management solution for Windows, recognized for its user-friendly approach, particularly suited for non-technical users. It simplifies the process of installing and updating multiple applications with just a few clicks. Users select their desired software from the Ninite website, which then creates a customized installer to install all chosen programs in one go, without any further interaction. Ninite stands out for its automated, batch installation process that smartly avoids toolbars or extra junk, ensuring a clean and hassle-free setup. It primarily caters to popular, commonly-used software, making it a convenient choice for quick setup of new computers or routine maintenance. The service is especially popular for its reliability and security, as it automatically updates all installed programs to their latest versions, reducing the risk of security vulnerabilities. Ninite's appeal lies in its simplicity and effectiveness, making it an excellent choice for those who prefer a straightforward, no-fuss software management tool.

Ninite is unique in several ways:

1. **User-Friendliness**: Ninite is known for its exceptionally user-friendly interface. It allows users to select multiple applications from its website and creates a single installer to install all chosen apps at once. This simplicity contrasts with other package managers, which often require command-line interaction.

2. **Silent Installation**: Ninite automates the installation process without requiring user interaction. It automatically declines toolbar installations and other add-ons, installs the latest version of the software, and sets up programs in their default locations. This automatic, 'silent' approach is different from others that might require user input during installations.

3. **Batch Updates**: Ninite can also be used for updating installed programs. It checks for and installs updates for all supported apps, again doing so silently and automatically. This is a distinctive feature that may not be as straightforward in other package managers.

4. **No Bundleware**: Ninite is known for not including any bundleware or extra software in its installations, which is a significant advantage over some other package managers that might include additional offers or software in their installations.

5. **Focus on Free and Popular Software**: Ninite primarily focuses on popular free software, making it a go-to for quickly setting up a new PC with essential tools and utilities. Other package managers may offer a wider range of software, including more niche or specialized tools.

6. **No Command-Line Interface (CLI)**: Unlike winget, Chocolatey, and Scoop, which offer command-line interfaces for more control and scripting capabilities, Ninite operates almost entirely through a graphical interface. This makes it more accessible to casual users but less flexible for power users.

7. **Automatic Background Updates (Pro Version)**: Ninite Pro offers additional features like automatic background updates and network deployment, which can be particularly useful in a corporate environment for maintaining multiple machines.

These features make Ninite especially appealing for less technical users or those looking for a hassle-free way to install and update a standard set of applications on their PCs. However, it may be less suitable for users who need a broader range of software options or more control over the installation process.

## Why Scoop Stands Out
Scoop stands out for a developer like me due to its primary focus on developer tools, making it an ideal choice for those needing a straightforward, command-line-centric tool for managing development applications. The user space installations are a key feature for individuals without administrator rights or those who prefer not to make system-wide changes, enhancing both ease of use and system stability. Additionally, Scoop's preference for portable applications and its Git-powered, manifest-driven approach offer a level of flexibility and transparency that is highly valued in a developer's workflow. Moreover, its design philosophy to avoid bloatware, coupled with its capabilities for customization and automation through scripting, make it an attractive option for users seeking minimalistic and efficient package management. While winget, Chocolatey, and Ninite each have their own strengths, Scoop's specific focus on minimal system impact, developer-friendly features, and ease of use in a command-line environment sets it apart as the go-to choice for many developers and command-line users.

{{< aside >}}
While one of Scoop's key features is its capability to install portable applications in a user-isolated space, Scoop also offers the option to install non-portable applications. This flexibility allows users who prefer to standardize on Scoop for application management to enjoy the convenience of using it for both portable and non-portable installations.
{{< /aside >}}

## Scoop Deep Dive
### Installing Scoop
Installing Scoop is a straightforward process. Here's a step-by-step guide:

1. **Open PowerShell**: First, you need to open PowerShell. You can do this by searching for PowerShell in the Start menu and running it as an administrator.

2. **Configure Execution Policy** (if needed): Scoop requires the ability to execute scripts. In PowerShell, run the following command to allow script execution:

   ```powershell
   Set-ExecutionPolicy RemoteSigned -scope CurrentUser
   ```

   This command changes the policy to allow the execution of scripts, while still protecting against the execution of scripts downloaded from the Internet, unless they are signed by a trusted publisher.

   {{< aside >}}
   **Note on Security**: Setting the PowerShell execution policy to `RemoteSigned` for the `CurrentUser` scope, as done with `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser`, poses a moderate security risk. This policy allows locally created scripts to run without a digital signature, but requires scripts downloaded from the internet to be signed by a trusted publisher. While this setting prevents the execution of unsigned scripts from external sources, it doesn't safeguard against all risks. Locally created or modified scripts, which might include malicious code, can still run without any signature verification. Additionally, attackers could potentially exploit this policy by tricking users into downloading and locally modifying scripts, thereby bypassing the need for a digital signature. This setting strikes a balance between usability and security, but users should remain cautious of the scripts they run, especially those obtained from untrusted sources.
   {{< /aside >}}

3. **Install Scoop**: To install Scoop, run the following command in PowerShell:

   ```powershell
   Invoke-RestMethod -Uri https://get.scoop.sh | Invoke-Expression
   ```

   This command downloads the Scoop installation script and executes it. `Invoke-RestMethod` fetches the script directly from `https://get.scoop.sh`, and `Invoke-Expression` runs the script.

4. **Install Git (Recommended)**: After installing Scoop, it is recommended to install Git. Many packages, including those in the 'extras' bucket, require Git. To install Git, use the following Scoop command:

   ```powershell
   scoop install git
   ```

5. **Verification**: After the installation script completes, you can verify the installation by typing `scoop help` in PowerShell. This should display a list of Scoop commands, indicating that Scoop is installed correctly.

6. **Optional - Configure Scoop to use a Proxy** (if needed): If you are behind a proxy, you may need to configure Scoop to use it. You can do this by setting the `http_proxy` and `https_proxy` environment variables in PowerShell:

   ```powershell
   $env:http_proxy="http://yourproxy:port"
   $env:https_proxy="http://yourproxy:port"
   ```

   Replace `yourproxy` and `port` with your proxy details.

7. **Optional - Associate `.git*` and `.sh` Files**: After installing Git, you can optionally create file associations for `.git*` and `.sh` files. This can be done by running the `install-file-associations.reg` file, which is included in the Git installation via Scoop.

    ```powershell
    cd path\to\git\installation
    & .\install-file-associations.reg
    ```

    Replace `path\to\git\installation` with the actual installation path of Git. This step is especially useful for developers who frequently work with Git and shell scripts.

Scoop is designed to make the installation process easy and straightforward, particularly for command-line tools and developer utilities. If you encounter any issues during the installation, make sure you're running PowerShell as an administrator and that your internet connection is stable. Scoop's GitHub page and community forums can also be useful resources for troubleshooting.

### Basic Commands
Scoop is a command-line package manager for Windows that offers a variety of commands for managing software installations. Here's a guide to some of the basic commands and their usage:

1. **Install a Package**: To install a package with Scoop, use the `install` command followed by the package name. For example, to install Visual Studio Code:

   ```powershell
   scoop install vscode
   ```

   {{< aside >}}
   **Note on Additional Customization**: Some packages, like Visual Studio Code, offer additional setup options such as creating context menu entries (right-click menu options) or file associations. After installing such packages, you might find additional scripts or instructions in the package’s installation folder to enable these features. This can enhance your workflow by integrating the software more deeply into your Windows environment. However, it's important to note that using these .reg files or scripts might deviate from the standard Scoop practice of keeping programs self-contained within the user folder. For detailed steps and to understand the implications of these modifications, refer to the documentation of the specific package or check the installation folder.
   {{< /aside >}}

2. **List Installed Packages**: To see a list of all packages that you've installed with Scoop, use the `list` command:

   ```powershell
   scoop list
   ```

3. **Search for a Package**: If you're not sure about the exact name of a package, you can search for it using the `search` command:

   ```powershell
   scoop search [package-name]
   ```

   Replace `[package-name]` with the name or part of the name of the package you're looking for.

4. **Check for Updates**: To check if any of your installed packages have updates available, use the `status` command:

   ```powershell
   scoop status
   ```

5. **Update a Package**: If there's an update available for a package, you can update it using the `update` command. To update Git, for example:

   ```powershell
   scoop update git
   ```

   To update all your installed packages at once, use:

   ```powershell
   scoop update *
   ```

6. **Uninstall a Package**: To remove a package that you no longer need, use the `uninstall` command:

   ```powershell
   scoop uninstall [package-name]
   ```

7. **View Package Information**: To view information about a specific package (like version, dependencies, etc.), use the `info` command:

   ```powershell
   scoop info [package-name]
   ```

8. **Add a Bucket**: Buckets in Scoop are like repositories that contain manifests for installing software. To add a new bucket, use the `bucket add` command. For example, to add the 'extras' bucket:

   ```powershell
   scoop bucket add extras
   ```

9. **List Buckets**: To see a list of all the buckets you've added, use the `bucket list` command:

   ```powershell
   scoop bucket list
   ```

10. **Cleanup Old Versions**: Over time, you may accumulate older versions of packages. To remove these old versions and free up disk space, use the `cleanup` command:

    ```powershell
    scoop cleanup [package-name]
    ```

    To cleanup old versions of all packages, simply run:

    ```powershell
    scoop cleanup *
    ```

These basic commands cover most of the everyday usage scenarios for Scoop. It's worth noting that Scoop is particularly popular among developers and power users who prefer a command-line interface for managing software installations. For more advanced commands and options, you can always refer to the Scoop documentation or use the `scoop help` command.

### Managing Multiple Versions
Scoop provides additional commands and features that are particularly useful for managing multiple versions of the same software. This capability is especially valuable for developers who may need to switch between different versions of tools or programming languages for different projects. Here are some key commands and concepts related to managing multiple versions in Scoop:

1. **Hold and Unhold a Version**: 
   - **Hold**: If you want to prevent a specific version of a package from being updated, you can 'hold' it. This ensures that the package stays at the current version even when you update other packages.
     ```powershell
     scoop hold [package-name]
     ```
   - **Unhold**: To remove the hold and allow updates again, use 'unhold':
     ```powershell
     scoop unhold [package-name]
     ```

2. **Install Specific Versions**: Scoop allows you to install specific versions of a package (if available in the bucket). Use the `install` command with the `@[version]` syntax:
   ```powershell
   scoop install [package-name]@[version]
   ```
   Replace `[version]` with the desired version number.

3. **Switch Between Versions**: If you have multiple versions of a package installed, you can switch between them using the `reset` command. This command sets the specified version as the current active version.
   ```powershell
   scoop reset [package-name]@[version]
   ```

4. **List All Versions of a Package**: To see all the installed versions of a particular package, use the `list` command with the package name:
   ```powershell
   scoop list [package-name]
   ```

5. **Check Available Versions**: To see what versions of a package are available for installation, you can use the `checkver` command with the `-a` (all) flag:
   ```powershell
   scoop checkver [package-name] -a
   ```

6. **Remove Specific Versions**: If you want to remove a specific version of a package, use the `uninstall` command with the specific version:
   ```powershell
   scoop uninstall [package-name]@[version]
   ```

These commands enhance Scoop's versatility, making it a powerful tool for scenarios where version management is crucial. It's particularly useful in development environments where testing across multiple versions of languages, libraries, or tools is necessary. Remember that the availability of different versions depends on the package and the bucket it belongs to. Not all packages may have multiple versions available in Scoop's default buckets.

### Buckets and Repositories
Customizing Scoop with buckets and repositories is a key aspect of its flexibility and power. Buckets in Scoop are analogous to repositories in systems like Git. They are collections of "manifests" (JSON files) that describe how to install each application. By adding and managing buckets, you can extend Scoop's range of available software and tailor it to your specific needs. Here's how to customize Scoop using buckets and repositories:

1. **Understanding Buckets**:
   - A Scoop bucket is a Git repository containing application manifests. Scoop uses these manifests to install applications.
   - By default, Scoop comes with its main bucket, which contains a wide range of commonly used applications.

2. **Adding Custom Buckets**:
   - To add a new bucket, use the `bucket add` command followed by the bucket's name and, optionally, the Git URL if it's a third-party bucket:
     ```powershell
     scoop bucket add [bucket-name] [bucket-url]
     ```
   - For well-known buckets, like 'extras', you don't need to specify the URL:
     ```powershell
     scoop bucket add extras
     ```
   - These commands add the specified bucket to your Scoop installation, making all the applications in that bucket available for installation.

3. **Listing Available Buckets**:
   - You can list all the buckets currently added to your Scoop installation with the `bucket list` command:
     ```powershell
     scoop bucket list
     ```

4. **Searching Across Buckets**:
   - Once you've added a bucket, you can search for applications across all your added buckets using the `search` command:
     ```powershell
     scoop search [application-name]
     ```

5. **Creating Your Own Bucket**:
   - If you have a set of applications or specific versions that aren't available in existing buckets, you can create your own. To do this, you need to:
     - Create a new Git repository.
     - Add application manifests in the form of JSON files to this repository.
     - Use the `bucket add` command to add your custom bucket to Scoop.

6. **Contributing to Existing Buckets**:
   - If you want to add a new application or update an existing one in a public bucket, you can fork the bucket's repository, make your changes, and then submit a pull request. This is common in open-source projects and allows for community contributions.

7. **Removing Buckets**:
   - If you no longer need a specific bucket, you can remove it from your Scoop installation using the `bucket rm` command:
     ```powershell
     scoop bucket rm [bucket-name]
     ```

Customizing Scoop with buckets allows you to significantly expand the range of software that you can manage with it. Whether you're adding existing third-party buckets, creating your own for unique needs, or contributing to the community, buckets provide a powerful way to tailor Scoop to your workflow.

## Conclusion
For my new machine, I've chosen Scoop as the primary installation tool, primarily because it neatly isolates application installations within my User folder. While I'm not strictly committed to using only one installer, I've found Scoop's approach efficient for most applications.

However, for certain software not available in Scoop's buckets, I'm open to using alternatives like winget or specific application installers. A case in point is Visual Studio, a crucial tool for some of my development work. For this, I utilized the Visual Studio Installer, which not only facilitates updating and managing different versions of Visual Studio but also simplifies the modification of workloads.

So far, my experience with Scoop has been positive. I anticipate that as I continue using it, I'll gain a deeper understanding of how separate installation paths can streamline software management on my machines. I encourage you to explore different package managers, especially if you've been relying on the same one or haven't used one at all. Diversifying your toolkit can be both enlightening and practical.

## Additional Resources
- [Use the winget tool to install and manage applications](https://learn.microsoft.com/en-us/windows/package-manager/winget/)
- [Chocolatey: The Package Manager for Windows](https://chocolatey.org/)
- [Chocolatey GUI](https://docs.chocolatey.org/en-us/chocolatey-gui/)
- [Scoop: A command-line installer for Windows](https://scoop.sh)
- [Ninite: Install and Update All Your Programs at Once](https://ninite.com/)
