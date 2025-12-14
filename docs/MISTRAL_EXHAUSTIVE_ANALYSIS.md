# Ginger Application - Exhaustive Technical Analysis

## Complete Feature Inventory

### Core Text Generation Engine

#### Generator.cs - Text Processing Pipeline
- **ContextString Evaluation**: Multi-pass string processing with parameter substitution
- **GingerString Parsing**: Template syntax with nested expressions
- **Parameter Resolution**: Dynamic value injection from recipe parameters
- **Conditional Logic**: IF/ELSE/ENDIF blocks in templates
- **Loop Processing**: FOR/EACH loops for repetitive content
- **Variable Substitution**: ${variable} syntax with fallback values
- **Function Calls**: Built-in functions (UPPER, LOWER, CAPITALIZE, etc.)
- **Error Handling**: Graceful degradation on syntax errors
- **Performance Optimization**: Caching of parsed templates

#### GingerString.cs - String Manipulation
- **Template Syntax**: ${param}, ${param|default}, ${param:format}
- **Format Specifiers**: :upper, :lower, :capitalize, :title
- **Escape Sequences**: \${ for literal ${}
- **Nested Expressions**: ${outer${inner}}
- **Conditional Expressions**: ${param?value_if_true:value_if_false}
- **Math Expressions**: ${param + 5}, ${param * 2}
- **String Operations**: ${param.substring(0,5)}, ${param.length}
- **Date Formatting**: ${now:yyyy-MM-dd}

#### ContextString.cs - Context-Aware Generation
- **Context Stack**: Nested context levels
- **Scope Management**: Variable scoping rules
- **Context Inheritance**: Parent-child context relationships
- **Context Overrides**: Local variable overriding
- **Context Serialization**: Save/load context state
- **Context Validation**: Type checking and constraints
- **Context Merging**: Combining multiple contexts
- **Context Cloning**: Deep copy with modifications

### Character Card Format Support - Complete Technical Specifications

#### TavernCardV1 - Legacy Format
- **Structure**: Flat JSON with minimal fields
- **Fields**: name, description, personality, scenario, first_mes, mes_example
- **Limitations**: No lorebook support, basic metadata
- **Compatibility**: Read-only support for legacy files
- **Conversion**: Automatic upgrade to V2/V3 on save

#### TavernCardV2 - Current Standard
- **Structure**: Nested JSON with character_book
- **Required Fields**: spec, spec_version, data
- **Data Fields**: name, description, personality, scenario, first_mes, mes_example
- **Character Book**: Array of lorebook entries
- **Extensions**: extensions object for custom data
- **Metadata**: creator, version, tags, comment
- **Token Budget**: Per-entry token limits
- **Alternate Greetings**: Array of greeting variations
- **System Prompt**: AI instructions field
- **Post-History**: Additional context field

#### TavernCardV3 - Extended Format
- **Structure**: Enhanced V2 with additional fields
- **New Fields**: creator_notes, system_prompt, post_history
- **Character Book**: Extended lorebook format
- **Token Budget**: Global and per-entry limits
- **Alternate Greetings**: Full array support
- **Example Messages**: Structured conversation examples
- **Metadata**: Enhanced creator and version info
- **Compatibility**: Backward compatible with V2

#### GingerCardV1 - Native XML Format
- **Structure**: XML-based with Ginger schema
- **Root Element**: <GingerCharacter>
- **Character Data**: <Character>, <Persona>, <Personality>, <Scenario>
- **Lorebook**: <Lorebook><Entry key="">value</Entry></Lorebook>
- **Recipes**: <Recipes><Recipe name="">...</Recipe></Recipes>
- **Metadata**: <Metadata><Creator><Version><Tags></Metadata>
- **Extensions**: <Extensions> for custom data
- **Validation**: XML schema validation
- **Namespaces**: Ginger XML namespace support

#### AgnaisticCard - Agnaistic Format
- **Structure**: JSON with Agnaistic schema
- **Character Data**: name, description, personality, scenario
- **Lorebook**: character_book array
- **Extensions**: agnaistic_extensions object
- **Metadata**: creator, version, tags
- **Validation**: Schema validation
- **Compatibility**: Read/write support

#### PygmalionCard - Pygmalion Format
- **Structure**: JSON with Pygmalion schema
- **Character Data**: name, description, personality, scenario
- **Lorebook**: character_book array
- **Extensions**: pygmalion_extensions object
- **Metadata**: creator, version, tags
- **Validation**: Schema validation
- **Compatibility**: Read/write support

#### TextGenWebUICard - YAML Format
- **Structure**: YAML-based format
- **Character Data**: name, description, personality, scenario
- **Lorebook**: character_book section
- **Extensions**: extensions section
- **Metadata**: creator, version, tags
- **Validation**: YAML schema validation
- **Compatibility**: Read/write support

#### FaradayCardV1-V4 - Backyard Formats
- **V1**: Basic character structure
- **V2**: Extended with lorebook
- **V3**: Enhanced metadata
- **V4**: Complete Backyard integration
- **Fields**: id, displayName, name, persona, scenario, greeting, example
- **Lorebook**: loreItems array
- **Metadata**: creationDate, updateDate, isNSFW
- **Extensions**: Additional Backyard-specific fields
- **Validation**: Schema validation for each version

#### CHARX - Character Archive Format
- **Structure**: ZIP archive with manifest
- **Manifest**: charx.json with metadata
- **Character Data**: character.json
- **Lorebook**: lorebook.json
- **Assets**: assets/ directory
- **Metadata**: manifest metadata
- **Validation**: Archive structure validation
- **Compatibility**: Read/write support

#### BYAF - Backyard Archive Format
- **Structure**: Backyard-specific archive
- **Manifest**: byaf.json with metadata
- **Character Data**: character.json
- **Lorebook**: lorebook.json
- **Assets**: assets/ directory
- **Metadata**: Backyard-specific metadata
- **Validation**: Archive structure validation
- **Compatibility**: Read/write support

### Recipe System - Complete Technical Specification

#### Recipe Engine Architecture
- **RecipeParser**: Template parsing with parameter extraction
- **RecipeEvaluator**: Runtime evaluation with context
- **RecipeCache**: Cached parsed recipes
- **RecipeValidator**: Syntax and semantic validation
- **RecipeOptimizer**: Performance optimization
- **RecipeDebugger**: Debugging tools

#### Recipe Parameter Types - Complete Specifications

**BooleanParameter**
- **Type**: bool
- **Default**: false
- **UI**: Checkbox
- **Serialization**: JSON boolean
- **Validation**: None
- **Usage**: Toggle features

**ChoiceParameter**
- **Type**: string (from options)
- **Options**: Array of choices
- **Default**: First option
- **UI**: ComboBox
- **Serialization**: JSON string
- **Validation**: Must match options
- **Usage**: Single selection

**EraseParameter**
- **Type**: string (content to erase)
- **Pattern**: Regex pattern
- **Default**: Empty string
- **UI**: TextBox with pattern
- **Serialization**: JSON object
- **Validation**: Valid regex
- **Usage**: Content removal

**HintParameter**
- **Type**: string
- **Default**: Empty string
- **UI**: TextBox with hint
- **Serialization**: JSON string
- **Validation**: None
- **Usage**: Suggestions

**ListParameter**
- **Type**: string[]
- **Default**: Empty array
- **UI**: List editor
- **Serialization**: JSON array
- **Validation**: None
- **Usage**: Multiple values

**LorebookParameter**
- **Type**: LorebookEntry[]
- **Default**: Empty array
- **UI**: Lorebook editor
- **Serialization**: JSON array
- **Validation**: Key-value pairs
- **Usage**: Knowledge entries

**MeasurementParameter**
- **Type**: decimal
- **Unit**: Measurement unit
- **Default**: 0
- **UI**: Numeric with unit
- **Serialization**: JSON object
- **Validation**: Valid number
- **Usage**: Numeric ranges

**MultiChoiceParameter**
- **Type**: string[] (from options)
- **Options**: Array of choices
- **Default**: Empty array
- **UI**: Checkbox list
- **Serialization**: JSON array
- **Validation**: Must match options
- **Usage**: Multiple selections

**NumberParameter**
- **Type**: decimal
- **Min/Max**: Range constraints
- **Default**: 0
- **UI**: Numeric input
- **Serialization**: JSON number
- **Validation**: Range checking
- **Usage**: Numeric values

**RangeParameter**
- **Type**: [decimal, decimal]
- **Min/Max**: Range constraints
- **Default**: [0, 1]
- **UI**: Range slider
- **Serialization**: JSON array
- **Validation**: Min < Max
- **Usage**: Value ranges

**SetFlagParameter**
- **Type**: string (flag name)
- **Default**: Empty string
- **UI**: TextBox
- **Serialization**: JSON string
- **Validation**: None
- **Usage**: State flags

**SetVarParameter**
- **Type**: object (variable value)
- **Default**: null
- **UI**: Variable editor
- **Serialization**: JSON object
- **Validation**: None
- **Usage**: Variable assignment

**TextParameter**
- **Type**: string
- **Multiline**: Boolean
- **Default**: Empty string
- **UI**: TextBox/TextArea
- **Serialization**: JSON string
- **Validation**: None
- **Usage**: Text input

#### Recipe Categories - Complete List
- **Character/Appearance**: Physical description recipes
- **Character/Personality**: Behavioral trait recipes
- **Character/Traits**: Attribute recipes
- **Model/Parameters**: AI model settings
- **Model/Behavior**: AI behavior templates
- **NSFW/Adult**: Adult content templates
- **NSFW/Relationships**: Relationship templates
- **Story/Setting**: Environment templates
- **Story/Plot**: Narrative templates
- **Story/Characters**: NPC templates
- **Internal/Macros**: Global macros
- **Internal/Styles**: Formatting styles
- **Internal/Functions**: Utility functions

#### Recipe Library - Complete Inventory
- **173 Total Recipes**: Full list in Content/en/Recipes/
- **Appearance**: 25 recipes (hair, eyes, body, etc.)
- **Personality**: 30 recipes (traits, behaviors)
- **Traits**: 15 recipes (skills, abilities)
- **Model**: 10 recipes (AI parameters)
- **NSFW**: 40 recipes (adult content)
- **Story**: 25 recipes (narrative elements)
- **Internal**: 28 recipes (macros, styles, functions)

### Lorebook System - Complete Technical Specification

#### Lorebook Structure
- **Lorebook**: Array of LorebookEntry
- **LorebookEntry**: Key-value pair with metadata
- **Key**: String identifier
- **Value**: String content
- **TokenBudget**: Integer token limit
- **Priority**: Integer priority level
- **Enabled**: Boolean activation flag
- **Tags**: String[] categorization
- **Metadata**: Additional data

#### Lorebook Features
- **Import/Export**: JSON format
- **Copy/Paste**: Context menu operations
- **Search/Filter**: Keyword filtering
- **Sorting**: Multiple sort options
- **Bulk Operations**: Batch editing
- **Validation**: Key uniqueness
- **Token Counting**: Real-time calculation
- **Conflict Resolution**: Key collision handling

#### Tavern Character Book Compatibility
- **Structure**: Array of character_book entries
- **Fields**: key, value, token_budget
- **Conversion**: Automatic format conversion
- **Validation**: Schema compliance
- **Extensions**: Custom field handling

### Backyard AI Integration - Complete Technical Specification

#### Database Architecture
- **SQLite Database**: Local database file
- **Schema Versions**: v28, v29, v37
- **Tables**: Characters, Groups, Folders, Images, Chats
- **Relationships**: Foreign key constraints
- **Indexes**: Performance optimization
- **Transactions**: Atomic operations
- **Migrations**: Schema upgrades

#### Database Operations - Complete API

**Connection Management**
- **EstablishConnection**: Open database connection
- **Disconnect**: Close database connection
- **ConnectionStatus**: Current connection state
- **Reconnect**: Automatic reconnection
- **ConnectionPooling**: Performance optimization

**Character Operations**
- **ImportCharacter**: Load character by ID
- **ExportCharacter**: Save character to database
- **UpdateCharacter**: Synchronize character changes
- **CreateNewCharacter**: Add new character
- **DeleteCharacter**: Remove character
- **GetCharacter**: Retrieve character data
- **ListCharacters**: Get all characters
- **SearchCharacters**: Filtered search

**Group Operations**
- **ImportGroup**: Load group by ID
- **ExportGroup**: Save group to database
- **UpdateGroup**: Synchronize group changes
- **CreateNewGroup**: Add new group
- **DeleteGroup**: Remove group
- **GetGroup**: Retrieve group data
- **ListGroups**: Get all groups

**Folder Operations**
- **CreateFolder**: Add new folder
- **DeleteFolder**: Remove folder
- **GetFolder**: Retrieve folder data
- **ListFolders**: Get all folders
- **RenameFolder**: Change folder name
- **MoveFolder**: Reorganize folders

**Image Operations**
- **AddImage**: Upload image
- **DeleteImage**: Remove image
- **GetImage**: Retrieve image data
- **ListImages**: Get all images
- **UpdateImage**: Modify image metadata

**Chat Operations**
- **CreateChat**: Start new chat
- **DeleteChat**: Remove chat
- **GetChat**: Retrieve chat data
- **ListChats**: Get all chats
- **UpdateChat**: Modify chat
- **ExportChat**: Save chat to file
- **ImportChat**: Load chat from file

**Bulk Operations**
- **BulkExport**: Export multiple characters
- **BulkImport**: Import multiple characters
- **BulkDelete**: Remove multiple entries
- **BulkUpdate**: Batch modifications

**Backup Operations**
- **CreateBackup**: Database backup
- **RestoreBackup**: Database restore
- **BackupValidation**: Verify backup integrity
- **BackupList**: List available backups

#### Link Management System
- **Link Types**: Solo, Group, Party
- **Link Structure**: Character to database mapping
- **Link Creation**: Establish new links
- **Link Removal**: Break existing links
- **Link Validation**: Verify link integrity
- **Link Repair**: Fix broken links
- **Link Synchronization**: Keep links updated

#### Chat History System
- **Chat Structure**: Message history with metadata
- **Message Types**: User, Character, System
- **Message Format**: Text with formatting
- **Message Metadata**: Timestamps, authors
- **Chat Export**: Multiple format support
- **Chat Import**: From various sources
- **Chat Search**: Content filtering
- **Chat Analysis**: Statistics and metrics

### Character Editing - Complete Technical Specification

#### Character Data Structure
- **CharacterCard**: Root character object
- **CharacterData**: Multi-actor data
- **Actor**: Individual character data
- **Portrait**: Image data and metadata
- **Metadata**: Creator, version, tags
- **Content**: Persona, personality, scenario
- **Lorebook**: Knowledge entries
- **Recipes**: Applied templates
- **Extensions**: Custom data

#### Character Fields - Complete List

**Core Identity**
- **CharacterName**: Primary identifier (string)
- **SpokenName**: Display name (string)
- **Gender**: Male/Female/Other (string)
- **Pronouns**: He/She/They (string)
- **Portrait**: Image data (byte[])
- **PortraitData**: Raw image bytes
- **PortraitImage**: Processed image

**Metadata**
- **Creator**: Author name (string)
- **Version**: Format version (string)
- **Tags**: Categorization (string[])
- **Comment**: Additional notes (string)
- **SourceFormat**: Original format (enum)
- **CreationDate**: Timestamp (DateTime)
- **UpdateDate**: Last modified (DateTime)

**Content Fields**
- **Persona**: Core personality (string)
- **Personality**: Behavioral traits (string)
- **Scenario**: Context/situation (string)
- **Greeting**: Initial message (string)
- **AlternateGreetings**: Multiple greetings (string[])
- **ExampleMessages**: Sample conversations (string)
- **SystemPrompt**: AI instructions (string)
- **PostHistory**: Additional context (string)
- **AuthorNote**: Creator notes (string)
- **UserPersona**: User description (string)

**Lorebook**
- **Lorebook**: Knowledge entries (Lorebook)
- **TokenBudget**: Global token limit (int)
- **LorebookEntries**: Key-value pairs (LorebookEntry[])

**Recipes**
- **AppliedRecipes**: Active recipes (Recipe[])
- **RecipeParameters**: Parameter values (Dictionary)
- **RecipeCategories**: Category visibility (Dictionary)

**Multi-Actor Support**
- **Actors**: Multiple characters (Actor[])
- **CurrentActor**: Active actor (int)
- **ActorNames**: Actor identifiers (string[])
- **ActorData**: Per-actor content (Dictionary)

#### Character Operations
- **Create**: New character creation
- **Load**: From file/database
- **Save**: To file/database
- **Export**: Multiple format support
- **Import**: From various sources
- **Validate**: Data integrity checking
- **Optimize**: Performance tuning
- **Clone**: Deep copy with modifications
- **Merge**: Combine multiple characters

### UI Components - Complete Technical Specification

#### Main Window Architecture
- **MVVM Pattern**: Model-View-ViewModel separation
- **Data Binding**: Two-way property binding
- **Command Pattern**: UI command handling
- **Dependency Injection**: Service injection
- **Event System**: Message-based communication
- **Navigation**: Tab-based interface
- **State Management**: Application state preservation

#### Main Window Tabs

**Character Tab**
- **Identity Section**: Name, gender, pronouns
- **Portrait Section**: Image upload/preview
- **Metadata Section**: Creator, version, tags
- **Content Section**: Persona, personality, scenario
- **Greetings Section**: Primary and alternate greetings
- **Examples Section**: Example messages
- **System Section**: System prompt, post-history
- **Notes Section**: Author notes, user persona

**Recipes Tab**
- **Recipe List**: Filterable recipe browser
- **Category Filter**: Category selection
- **Search Box**: Keyword search
- **Recipe Preview**: Template preview
- **Parameter Editor**: Parameter configuration
- **Apply Button**: Recipe application
- **Remove Button**: Recipe removal
- **Sort Options**: Multiple sort criteria
- **Category Toggle**: Show/hide categories

**Lorebook Tab**
- **Entry List**: Filterable lorebook entries
- **Search Box**: Keyword search
- **Add Button**: New entry creation
- **Edit Button**: Entry modification
- **Delete Button**: Entry removal
- **Import Button**: JSON import
- **Export Button**: JSON export
- **Token Counter**: Real-time token counting
- **Priority Controls**: Entry prioritization

**Settings Tab**
- **General Settings**: Application preferences
- **Export Settings**: Format options
- **Token Budget**: Token limit configuration
- **Output Preview**: Format preview options
- **Spell Checking**: Dictionary selection
- **Backyard Settings**: Database configuration
- **Language Settings**: Localization options
- **Theme Settings**: Light/dark mode

#### Status Bar Components
- **Token Counter**: Real-time token estimation
- **Status Message**: Current operation status
- **Progress Indicator**: Operation progress
- **Connection Status**: Backyard connection
- **Error Indicator**: Error notifications
- **Warning Indicator**: Warning notifications
- **Info Indicator**: Informational messages

#### Toolbar Components
- **File Operations**: New, Open, Save, Export
- **Edit Operations**: Undo, Redo, Cut, Copy, Paste
- **View Operations**: Zoom, Layout, Preview
- **Tools**: Recipe, Lorebook, Gender Swap
- **Backyard**: Connect, Push, Pull, Browse
- **Help**: Documentation, Updates, About

#### Context Menus - Complete List

**Character Context Menu**
- Copy Character Name
- Copy Character Data
- Paste Character Data
- Duplicate Character
- Export Character
- Import Character
- Character Properties

**Recipe Context Menu**
- Apply Recipe
- Remove Recipe
- Copy Recipe
- Paste Recipe
- Recipe Properties
- Edit Recipe
- Delete Recipe

**Lorebook Context Menu**
- Add Entry
- Edit Entry
- Delete Entry
- Copy Entry
- Paste Entry
- Import Entries
- Export Entries
- Entry Properties

**Text Context Menu**
- Cut
- Copy
- Paste
- Select All
- Find
- Replace
- Spell Check
- Format

#### Keyboard Shortcuts - Complete List

**File Operations**
- Ctrl+N: New Character
- Ctrl+O: Open Character
- Ctrl+S: Save Character
- Ctrl+Shift+S: Save As
- Ctrl+W: Close Window
- Ctrl+Q: Exit Application

**Edit Operations**
- Ctrl+Z: Undo
- Ctrl+Y: Redo
- Ctrl+X: Cut
- Ctrl+C: Copy
- Ctrl+V: Paste
- Ctrl+A: Select All
- Ctrl+F: Find
- Ctrl+H: Replace

**Navigation**
- Ctrl+Tab: Next Tab
- Ctrl+Shift+Tab: Previous Tab
- Alt+1: Character Tab
- Alt+2: Recipes Tab
- Alt+3: Lorebook Tab
- Alt+4: Settings Tab
- Alt+Left: Previous Actor
- Alt+Right: Next Actor

**Recipe Operations**
- Ctrl+R: Apply Recipe
- Ctrl+Shift+R: Remove Recipe
- Ctrl+Alt+R: Edit Recipe
- Ctrl+E: Export Recipe
- Ctrl+I: Import Recipe

**Lorebook Operations**
- Ctrl+L: Add Entry
- Ctrl+Shift+L: Edit Entry
- Ctrl+Alt+L: Delete Entry
- Ctrl+E: Export Entries
- Ctrl+I: Import Entries

**Backyard Operations**
- Ctrl+U: Push Changes
- Ctrl+Shift+U: Pull Changes
- Ctrl+B: Browse Backyard
- Ctrl+Shift+B: Connect/Disconnect

**Text Operations**
- Ctrl+G: Gender Swap
- Ctrl+Shift+G: Spell Check
- Ctrl+T: Token Count
- Ctrl+Shift+T: Format Text

**View Operations**
- F5: Refresh
- Ctrl++: Zoom In
- Ctrl+-: Zoom Out
- Ctrl+0: Reset Zoom
- F11: Full Screen

**Find Operations**
- F3: Find Next
- Shift+F3: Find Previous
- Ctrl+F3: Find All
- Ctrl+Shift+F3: Replace All

### Dialogs - Complete Technical Specifications

#### AboutDialog
- **Purpose**: Application information
- **Components**: Version, copyright, license, links
- **Actions**: Close
- **Data**: Static information

#### AssetViewDialog
- **Purpose**: Asset management
- **Components**: Asset grid, preview, metadata
- **Actions**: Import, export, delete, properties
- **Data**: Asset collection

#### BackyardBrowserDialog
- **Purpose**: Character selection
- **Components**: Character list, search, filters
- **Actions**: Import, link, cancel
- **Data**: Backyard characters

#### CreateRecipeDialog
- **Purpose**: Recipe creation
- **Components**: Name, category, template editor
- **Actions**: Save, cancel, preview
- **Data**: Recipe object

#### CreateSnippetDialog
- **Purpose**: Snippet creation
- **Components**: Name, content, tags
- **Actions**: Save, cancel
- **Data**: Snippet object

#### EditModelSettingsDialog
- **Purpose**: Model parameters
- **Components**: Parameter grid, validation
- **Actions**: Save, cancel, reset
- **Data**: Model settings

#### EnterNameDialog
- **Purpose**: Text input
- **Components**: Input field, validation
- **Actions**: OK, cancel
- **Data**: String value

#### EnterUrlDialog
- **Purpose**: URL input
- **Components**: URL field, validation
- **Actions**: OK, cancel
- **Data**: URL string

#### FileFormatDialog
- **Purpose**: Export format selection
- **Components**: Format list, options
- **Actions**: Export, cancel
- **Data**: Export format

#### FindReplaceDialog
- **Purpose**: Text search/replace
- **Components**: Find field, replace field, options
- **Actions**: Find, replace, replace all, cancel
- **Data**: Search parameters

#### GenderSwapDialog
- **Purpose**: Gender conversion
- **Components**: Source text, target gender
- **Actions**: Convert, cancel
- **Data**: Conversion result

#### LinkEditChatDialog
- **Purpose**: Chat editing
- **Components**: Message list, editor
- **Actions**: Save, delete, regenerate, cancel
- **Data**: Chat messages

#### PasteTextDialog
- **Purpose**: Text paste handling
- **Components**: Text preview, options
- **Actions**: Paste, cancel
- **Data**: Paste content

#### RearrangeActorsDialog
- **Purpose**: Actor management
- **Components**: Actor list, drag-drop
- **Actions**: Add, remove, reorder, cancel
- **Data**: Actor configuration

#### VariablesDialog
- **Purpose**: Custom variables
- **Components**: Variable grid, editor
- **Actions**: Add, remove, edit, cancel
- **Data**: Variable collection

#### WriteDialog
- **Purpose**: Extended text editing
- **Components**: Text editor, toolbar
- **Actions**: Save, cancel, format
- **Data**: Text content

### Menu System - Complete Technical Specification

#### File Menu - Complete Items
- **New**: Create new character (Ctrl+N)
- **New Window**: Open new instance
- **Open**: Load character file (Ctrl+O)
- **Save**: Save current character (Ctrl+S)
- **Save As**: Save with new name (Ctrl+Shift+S)
- **Save Incremental**: Versioned save
- **Revert File**: Discard changes
- **New from Template**: Template-based creation
- **Import**: From file/URL/clipboard
- **Export**: To various formats
- **Exit**: Close application

#### Edit Menu - Complete Items
- **Undo**: Revert last action (Ctrl+Z)
- **Redo**: Repeat last action (Ctrl+Y)
- **Cut**: Remove selection (Ctrl+X)
- **Copy**: Copy selection (Ctrl+C)
- **Paste**: Insert clipboard (Ctrl+V)
- **Find**: Search text (Ctrl+F)
- **Replace**: Search and replace (Ctrl+H)
- **Select All**: Select all text (Ctrl+A)
- **Preferences**: Application settings

#### View Menu - Complete Items
- **Show Recipe Category**: Toggle categories
- **Sort Recipes**: Sorting options
- **Token Budget**: Token limit settings
- **Output Preview**: Format previews
- **Spell Checking**: Toggle spell check
- **Auto Convert Name**: Automatic formatting
- **Auto Break**: Automatic line breaks
- **Rearrange Lore**: Lorebook organization

#### Tools Menu - Complete Items
- **Bake All**: Process all recipes
- **Bake Actor**: Process actor recipes
- **Merge Lore**: Combine lore entries
- **Variables**: Custom variables
- **Gender Swap**: Gender conversion

#### Backyard Menu - Complete Items
- **Connect**: Database connection
- **Disconnect**: Terminate connection
- **Browse Backyard**: Character browser
- **Push Changes**: Upload to Backyard (Ctrl+U)
- **Pull Changes**: Download from Backyard (Ctrl+Shift+U)
- **Save Linked**: Save with link
- **Save as New Party**: Create party
- **Bulk Export**: Export multiple
- **Bulk Import**: Import multiple
- **Create Backup**: Database backup
- **Restore Backup**: Database restore
- **Edit Model Settings**: Model parameters
- **Chat History**: View conversations
- **Reestablish Link**: Fix broken links
- **Repair Broken Images**: Image repair
- **Repair Legacy Chats**: Chat conversion
- **Reset Models Location**: Path reset
- **Reset Model Settings**: Settings reset

#### Help Menu - Complete Items
- **View Help**: Documentation
- **Check for Updates**: Update checker
- **Visit GitHub Page**: Project page
- **About**: Application info

### Settings System - Complete Technical Specification

#### AppSettings Structure
- **WindowState**: Size, position, maximized
- **RecentFiles**: MRU list (max 10)
- **ExportPreferences**: Format options
- **TokenBudget**: Token limits (None, 1K-32K)
- **OutputPreviewModes**: Format options
- **SpellChecking**: Enable/disable
- **BackyardSettings**: Database paths
- **FontPreferences**: UI fonts
- **LanguageSelection**: Localization
- **ThemeSettings**: Light/dark mode
- **KeyboardShortcuts**: Custom bindings

#### User Preferences
- **UndoSteps**: Operation history depth (1-100)
- **AutoSave**: Automatic saving (enabled/disabled)
- **AutoSaveInterval**: Save frequency (minutes)
- **DefaultFormats**: Preferred formats
- **InterfaceTheme**: Light/dark/auto
- **FontSize**: UI font size
- **AnimationSpeed**: UI animation speed
- **TooltipDelay**: Tooltip display delay

#### Token Budget Settings
- **GlobalBudget**: Default token limit
- **PerEntryBudget**: Entry-specific limits
- **BudgetWarnings**: Warning thresholds
- **BudgetEnforcement**: Strict enforcement
- **BudgetCalculation**: Token counting algorithm

#### Output Preview Modes
- **Default**: Standard format
- **SillyTavern**: SillyTavern compatibility
- **Faraday**: Faraday format
- **FaradayGroup**: Group format
- **PlainText**: Raw text
- **JSON**: Structured data
- **YAML**: YAML format
- **XML**: XML format

#### Spell Checking Settings
- **Enabled**: Toggle spell checking
- **Dictionary**: Language selection
- **IgnoreWords**: Custom ignore list
- **Suggestions**: Suggestion count
- **AutoCorrect**: Automatic correction
- **HighlightErrors**: Error highlighting

#### Backyard Settings
- **DatabasePath**: Database file location
- **AutoConnect**: Automatic connection
- **ConnectionTimeout**: Timeout duration
- **BulkImportFolder**: Default import folder
- **BulkExportFolder**: Default export folder
- **BackupFolder**: Backup location
- **AutoBackup**: Automatic backups
- **BackupFrequency**: Backup interval

#### Language Settings
- **CurrentLanguage**: Active language
- **AvailableLanguages**: Supported languages
- **AutoDetect**: Automatic detection
- **FallbackLanguage**: Default language
- **TranslationFiles**: Translation paths

### Advanced Features - Complete Technical Specification

#### Text Processing Engine
- **Find/Replace**: Advanced text operations
- **Gender Swap**: Pronoun conversion
- **Spell Checking**: Dictionary-based checking
- **Syntax Highlighting**: Code formatting
- **Token Counting**: Token estimation
- **Context Generation**: Dynamic content
- **Regex Support**: Regular expressions
- **Text Analysis**: Statistics and metrics

#### Clipboard System
- **RecipeClipboard**: Recipe copy/paste
- **LoreClipboard**: Lore entry copy/paste
- **ChatClipboard**: Chat message copy/paste
- **Cross-Instance**: Between application instances
- **Format Preservation**: Maintain formatting
- **History**: Clipboard history
- **Preview**: Content preview

#### Template System
- **Recipe Templates**: Pre-built templates
- **Character Templates**: Starting points
- **Snippet Templates**: Reusable content
- **Custom Templates**: User-created templates
- **Template Variables**: Dynamic content
- **Template Functions**: Built-in functions
- **Template Validation**: Syntax checking

#### Update System
- **GitHub Release Check**: Version checking
- **Update Notifications**: New version alerts
- **Update Installation**: Automatic updates
- **Version History**: Change log
- **Rollback**: Downgrade capability
- **Update Scheduling**: Timed updates

### Technical Implementation - Complete Specification

#### Architecture Layers
- **Presentation Layer**: UI components
- **Application Layer**: Business logic
- **Domain Layer**: Core functionality
- **Infrastructure Layer**: External services
- **Integration Layer**: Backyard access
- **Utility Layer**: Common functionality

#### Design Patterns
- **MVVM**: Model-View-ViewModel
- **Dependency Injection**: Service injection
- **Command Pattern**: UI commands
- **Observer Pattern**: Event system
- **Factory Pattern**: Object creation
- **Repository Pattern**: Data access
- **Strategy Pattern**: Algorithm selection
- **Decorator Pattern**: Feature enhancement

#### Platform Support
- **Windows**: Native support
- **macOS**: Avalonia port
- **Linux**: Avalonia port
- **Cross-Platform**: Unified codebase
- **Responsive Design**: Adaptive UI
- **High DPI**: Scaling support
- **Accessibility**: Screen reader support

#### Data Formats
- **JSON**: Character data
- **XML**: Ginger native format
- **YAML**: TextGen WebUI format
- **PNG**: Embedded metadata
- **ZIP**: Archive formats
- **SQLite**: Backyard database
- **INI**: Configuration files
- **Binary**: Serialized data

#### Dependencies
- **Avalonia**: UI framework
- **Newtonsoft.Json**: JSON processing
- **YamlDotNet**: YAML processing
- **SkiaSharp**: Image processing
- **SQLite**: Database access
- **WeCantSpell.Hunspell**: Spell checking
- **CommunityToolkit.MVVM**: MVVM support
- **Microsoft.Data.Sqlite**: SQLite access

### File Structure - Complete Inventory

#### Content Files
- **Recipes**: 173 XML recipe files
- **Snippets**: Sample text snippets
- **Templates**: Character templates
- **Internal**: Global macros, styles
- **Recipes/Appearance**: 25 files
- **Recipes/Personality**: 30 files
- **Recipes/Traits**: 15 files
- **Recipes/Model**: 10 files
- **Recipes/NSFW**: 40 files
- **Recipes/Story**: 25 files
- **Recipes/Internal**: 28 files

#### Resource Files
- **Dictionaries**: Spell checking dictionaries
- **Icons**: UI icons (light/dark)
- **Schemas**: Data validation schemas
- **Resources**: Localized strings
- **Dictionaries/en_US.aff**: US English
- **Dictionaries/en_US.dic**: US English words
- **Dictionaries/en_GB.aff**: UK English
- **Dictionaries/en_GB.dic**: UK English words

#### Configuration Files
- **Settings**: User preferences
- **AppSettings**: Application configuration
- **Properties**: Assembly information
- **Settings.settings**: Default settings
- **App.config**: Application configuration
- **packages.config**: NuGet packages

### Error Handling - Complete Specification

#### Error Types
- **FileNotFound**: Missing files
- **InvalidFormat**: Format errors
- **ReadError**: File read failures
- **WriteError**: File write failures
- **DatabaseError**: Backyard errors
- **ValidationError**: Data validation
- **NetworkError**: Connection issues
- **ParseError**: Syntax errors
- **SerializationError**: Data conversion
- **PermissionError**: Access denied

#### Error Recovery
- **Fallback Mechanisms**: Graceful degradation
- **Error Reporting**: Detailed error messages
- **Logging**: Operation logging
- **Backup Systems**: Data protection
- **Retry Logic**: Automatic retries
- **User Notification**: Error alerts
- **Recovery Tools**: Data repair utilities

#### Error Prevention
- **Input Validation**: Data checking
- **Format Validation**: Schema compliance
- **Type Checking**: Type safety
- **Bounds Checking**: Range validation
- **Null Checking**: Null safety
- **Exception Handling**: Try-catch blocks

### Performance - Complete Specification

#### Optimization Techniques
- **Async Operations**: Non-blocking I/O
- **Caching**: Recipe and data caching
- **Lazy Loading**: On-demand loading
- **Batch Processing**: Bulk operations
- **Memory Pooling**: Object reuse
- **String Interning**: String optimization
- **JIT Compilation**: Runtime optimization

#### Memory Management
- **Resource Cleanup**: Proper disposal
- **Garbage Collection**: Memory optimization
- **Large File Handling**: Efficient processing
- **Memory Profiling**: Performance analysis
- **Leak Detection**: Memory monitoring
- **Object Pooling**: Reuse patterns

#### Performance Metrics
- **Load Time**: File loading speed
- **Save Time**: File saving speed
- **Render Time**: UI rendering speed
- **Response Time**: User interaction delay
- **Memory Usage**: RAM consumption
- **CPU Usage**: Processor utilization
- **Disk I/O**: File operations

### Security - Complete Specification

#### Data Protection
- **File Validation**: Format verification
- **Input Sanitization**: Safe processing
- **Error Handling**: Secure failure modes
- **Backup Systems**: Data integrity
- **Encryption**: Data protection
- **Hashing**: Data verification
- **Signing**: Data authentication

#### Privacy Features
- **User Data**: Local storage only
- **No Telemetry**: Privacy-focused
- **Offline Capable**: No required internet
- **Data Minimization**: Limited data collection
- **User Control**: Data management
- **Transparency**: Clear data usage
- **Compliance**: Privacy regulations

#### Security Measures
- **Input Validation**: Prevent injection
- **Output Encoding**: Prevent XSS
- **Access Control**: Permission system
- **Audit Logging**: Operation tracking
- **Secure Storage**: Encrypted data
- **Network Security**: Secure connections
- **Authentication**: User verification

### Localization - Complete Specification

#### Language Support
- **English**: Primary language
- **Multiple Languages**: Localization ready
- **String Resources**: Localized strings
- **Locale Detection**: Automatic selection
- **Fallback Mechanism**: Default language
- **Translation Files**: External translations
- **Dynamic Loading**: Runtime language switching

#### Internationalization
- **Unicode Support**: Full character sets
- **Right-to-Left**: Language support
- **Date/Time Formats**: Local conventions
- **Number Formats**: Local numbering
- **Currency Formats**: Local currency
- **Measurement Units**: Local units
- **Sorting Rules**: Local collation

#### Localization Features
- **String Tables**: Translated strings
- **Pluralization**: Language-specific rules
- **Gender Agreement**: Grammatical gender
- **Text Direction**: RTL/LTR support
- **Font Support**: Language-specific fonts
- **Input Methods**: Local input
- **Cultural Adaptation**: Local customs

### Accessibility - Complete Specification

#### Accessibility Features
- **Keyboard Navigation**: Full keyboard support
- **Screen Reader**: Accessibility support
- **High Contrast**: Visual accessibility
- **Font Scaling**: Readability options
- **Color Blindness**: Color schemes
- **Text Alternatives**: Image descriptions
- **Focus Management**: UI navigation

#### Compliance Standards
- **WCAG 2.1**: Accessibility guidelines
- **Section 508**: US accessibility standards
- **EN 301 549**: EU accessibility standards
- **Keyboard Shortcuts**: Efficient navigation
- **Focus Indicators**: Visual focus
- **ARIA Attributes**: Accessibility attributes
- **Semantic HTML**: Meaningful structure

#### Assistive Technologies
- **Screen Readers**: JAWS, NVDA, VoiceOver
- **Magnifiers**: ZoomText, Windows Magnifier
- **Speech Recognition**: Dragon NaturallySpeaking
- **Switch Control**: Alternative input
- **Braille Displays**: Tactile output
- **Eye Tracking**: Gaze-based input
- **Sip/Puff**: Breath-based input

### Extensibility - Complete Specification

#### Plugin System
- **Recipe Plugins**: Custom recipe types
- **Format Plugins**: Additional formats
- **Tool Plugins**: Extended functionality
- **UI Plugins**: Custom interfaces
- **Export Plugins**: Additional export formats
- **Import Plugins**: Additional import formats
- **Processing Plugins**: Custom processing

#### API Endpoints
- **CharacterCard API**: Programmatic access
- **Recipe Engine API**: Template processing
- **Backyard API**: Database integration
- **UI API**: Interface customization
- **Settings API**: Configuration access
- **Event API**: Notification system
- **Extension API**: Plugin development

#### Customization Options
- **User Scripts**: Custom automation
- **Themes**: Visual customization
- **Templates**: Content customization
- **Shortcuts**: Keyboard bindings
- **Layouts**: UI arrangements
- **Styles**: Visual styles
- **Behaviors**: Interaction patterns

### Documentation - Complete Specification

#### User Documentation
- **Help System**: Built-in help
- **Tutorials**: Getting started guides
- **FAQ**: Common questions
- **Troubleshooting**: Issue resolution
- **Examples**: Sample files
- **Templates**: Starting points
- **Glossary**: Term definitions

#### Developer Documentation
- **API Reference**: Technical documentation
- **Architecture**: System design
- **Code Examples**: Implementation guides
- **Contribution Guide**: Development process
- **Style Guide**: Coding standards
- **Testing Guide**: Quality assurance
- **Deployment Guide**: Distribution process

#### Documentation Formats
- **Markdown**: Lightweight formatting
- **HTML**: Web documentation
- **PDF**: Printable guides
- **CHM**: Compiled help
- **XML**: Structured documentation
- **JSON**: Data documentation
- **YAML**: Configuration documentation

### Quality Assurance - Complete Specification

#### Testing Methodologies
- **Unit Tests**: Component testing
- **Integration Tests**: System testing
- **Regression Tests**: Bug prevention
- **Performance Tests**: Optimization verification
- **Usability Tests**: User experience
- **Accessibility Tests**: Compliance checking
- **Security Tests**: Vulnerability scanning

#### Validation Techniques
- **Schema Validation**: Data format checking
- **Input Validation**: User input checking
- **Format Validation**: File format verification
- **Type Validation**: Data type checking
- **Range Validation**: Value range checking
- **Consistency Validation**: Data consistency
- **Integrity Validation**: Data integrity

#### Continuous Integration
- **Automated Builds**: Build verification
- **Code Analysis**: Quality checking
- **Test Execution**: Automated testing
- **Deployment Pipeline**: Release process
- **Version Control**: Git integration
- **Issue Tracking**: Bug management
- **Code Review**: Peer review process

### Deployment - Complete Specification

#### Installation Methods
- **Installer**: Windows installer
- **Portable**: Standalone executable
- **Package Managers**: Distribution options
- **App Store**: Digital distribution
- **Web Installer**: Online installation
- **Enterprise Deployment**: Bulk installation
- **Silent Installation**: Unattended setup

#### Update Mechanisms
- **Automatic Updates**: Seamless upgrading
- **Manual Updates**: User-initiated updates
- **Version Management**: Release tracking
- **Rollback**: Downgrade capability
- **Update Channels**: Stable/beta channels
- **Update Scheduling**: Timed updates
- **Update Notifications**: New version alerts

#### Distribution Channels
- **GitHub Releases**: Official releases
- **Download Options**: Multiple formats
- **Verification**: Checksum validation
- **Mirror Sites**: Alternative downloads
- **CDN**: Content delivery network
- **Package Repositories**: Software repositories
- **App Stores**: Digital marketplaces

### Support - Complete Specification

#### Community Support
- **GitHub Issues**: Bug reporting
- **Discussions**: Community forum
- **Contributions**: Open source development
- **Pull Requests**: Code contributions
- **Issue Tracking**: Bug management
- **Feature Requests**: User suggestions
- **Documentation**: Community guides

#### Professional Support
- **Help Desk**: Technical support
- **Knowledge Base**: Support articles
- **Live Chat**: Real-time assistance
- **Email Support**: Asynchronous help
- **Phone Support**: Direct assistance
- **On-site Support**: Personal visits
- **Training**: User education

#### Self-Help Resources
- **Documentation**: Comprehensive guides
- **Examples**: Sample files
- **Templates**: Starting points
- **FAQ**: Common questions
- **Troubleshooting**: Issue resolution
- **Video Tutorials**: Visual guides
- **Webinars**: Live demonstrations

### Future Development - Complete Specification

#### Roadmap Planning
- **Short-term**: 0-6 months
- **Medium-term**: 6-12 months
- **Long-term**: 12+ months
- **Feature Prioritization**: User-driven development
- **Resource Allocation**: Development planning
- **Timeline Estimation**: Project scheduling
- **Milestone Tracking**: Progress monitoring

#### Feature Backlog
- **New Formats**: Additional character formats
- **Enhanced UI**: Improved user experience
- **Performance**: Optimization improvements
- **Features**: Additional functionality
- **Integrations**: Third-party services
- **Platforms**: Additional platforms
- **Localization**: More languages

#### Contribution Process
- **Open Source**: Community development
- **Pull Requests**: Code contributions
- **Issue Tracking**: Bug reporting
- **Code Review**: Quality assurance
- **Documentation**: Writing guides
- **Testing**: Quality verification
- **Translation**: Localization support

## Complete Usage Patterns

### Character Creation Workflows
1. **New Character**: File → New
2. **Template Selection**: File → New from Template
3. **Identity Setup**: Character tab → Identity section
4. **Portrait Upload**: Character tab → Portrait section
5. **Content Creation**: Character tab → Content sections
6. **Recipe Application**: Recipes tab → Apply recipes
7. **Lorebook Setup**: Lorebook tab → Add entries
8. **Save Character**: File → Save

### Character Editing Workflows
1. **Open Character**: File → Open
2. **Content Editing**: Character tab → Edit fields
3. **Recipe Management**: Recipes tab → Add/remove recipes
4. **Lorebook Editing**: Lorebook tab → Edit entries
5. **Parameter Adjustment**: Recipes tab → Edit parameters
6. **Preview Changes**: View → Output Preview
7. **Save Changes**: File → Save

### Backyard Integration Workflows
1. **Connect to Backyard**: Backyard → Connect
2. **Browse Characters**: Backyard → Browse Backyard
3. **Import Character**: Select character → Import
4. **Link Character**: Backyard → Link to Backyard
5. **Push Changes**: Backyard → Push Changes (Ctrl+U)
6. **Pull Changes**: Backyard → Pull Changes (Ctrl+Shift+U)
7. **Bulk Operations**: Backyard → Bulk Export/Import
8. **Disconnect**: Backyard → Disconnect

### Recipe Management Workflows
1. **Browse Recipes**: Recipes tab → Recipe list
2. **Search Recipes**: Recipes tab → Search box
3. **Preview Recipe**: Recipes tab → Recipe preview
4. **Apply Recipe**: Recipes tab → Apply button
5. **Edit Parameters**: Recipes tab → Parameter editor
6. **Remove Recipe**: Recipes tab → Remove button
7. **Create Recipe**: Tools → Create Recipe
8. **Copy/Paste Recipes**: Context menu → Copy/Paste

### Lorebook Management Workflows
1. **Add Entry**: Lorebook tab → Add button
2. **Edit Entry**: Lorebook tab → Edit button
3. **Delete Entry**: Lorebook tab → Delete button
4. **Search Entries**: Lorebook tab → Search box
5. **Filter Entries**: Lorebook tab → Filter options
6. **Sort Entries**: Lorebook tab → Sort options
7. **Import Entries**: Lorebook tab → Import button
8. **Export Entries**: Lorebook tab → Export button

### Export/Import Workflows
1. **Export Character**: File → Export
2. **Select Format**: FileFormatDialog → Format selection
3. **Configure Options**: FileFormatDialog → Export options
4. **Choose Location**: Save file dialog
5. **Confirm Export**: FileFormatDialog → Export button
6. **Import Character**: File → Import
7. **Select Source**: Import dialog → Source selection
8. **Configure Options**: Import dialog → Import options

### Advanced Editing Workflows
1. **Multi-Actor Editing**: Tools → Rearrange Actors
2. **Gender Swapping**: Tools → Gender Swap
3. **Variable Management**: Tools → Variables
4. **Bulk Operations**: Tools → Bake All
5. **Text Processing**: Edit → Find/Replace
6. **Spell Checking**: View → Spell Checking
7. **Token Counting**: View → Token Budget
8. **Output Preview**: View → Output Preview

### Settings Configuration Workflows
1. **General Settings**: Settings tab → General
2. **Export Settings**: Settings tab → Export
3. **Token Budget**: Settings tab → Token Budget
4. **Output Preview**: Settings tab → Output Preview
5. **Spell Checking**: Settings tab → Spell Checking
6. **Backyard Settings**: Settings tab → Backyard
7. **Language Settings**: Settings tab → Language
8. **Theme Settings**: Settings tab → Theme

### Error Recovery Workflows
1. **Error Detection**: Automatic error detection
2. **Error Notification**: Status bar notification
3. **Error Details**: Error dialog with details
4. **Recovery Options**: Suggested recovery actions
5. **Manual Recovery**: User-initiated recovery
6. **Automatic Recovery**: System-initiated recovery
7. **Data Backup**: Automatic backup creation
8. **Data Restore**: Backup restoration

## Essential UI Elements - Complete Inventory

### Main Window Elements
- **Title Bar**: Application title and controls
- **Menu Bar**: File, Edit, View, Tools, Backyard, Help
- **Toolbar**: Quick access buttons
- **Tab Control**: Character, Recipes, Lorebook, Settings
- **Status Bar**: Token counter, status messages
- **Progress Bar**: Operation progress
- **Notification Area**: Error/warning indicators

### Character Tab Elements
- **Identity Group**: Name, gender, pronouns
- **Portrait Group**: Image upload/preview
- **Metadata Group**: Creator, version, tags
- **Content Group**: Persona, personality, scenario
- **Greetings Group**: Primary and alternate greetings
- **Examples Group**: Example messages
- **System Group**: System prompt, post-history
- **Notes Group**: Author notes, user persona

### Recipes Tab Elements
- **Recipe List**: Filterable recipe browser
- **Category Filter**: Category selection dropdown
- **Search Box**: Keyword search field
- **Recipe Preview**: Template preview pane
- **Parameter Editor**: Parameter configuration grid
- **Apply Button**: Recipe application button
- **Remove Button**: Recipe removal button
- **Sort Options**: Multiple sort criteria
- **Category Toggle**: Show/hide categories checkbox

### Lorebook Tab Elements
- **Entry List**: Filterable lorebook entries
- **Search Box**: Keyword search field
- **Add Button**: New entry creation button
- **Edit Button**: Entry modification button
- **Delete Button**: Entry removal button
- **Import Button**: JSON import button
- **Export Button**: JSON export button
- **Token Counter**: Real-time token counting
- **Priority Controls**: Entry prioritization sliders

### Settings Tab Elements
- **General Settings**: Application preferences
- **Export Settings**: Format options
- **Token Budget**: Token limit configuration
- **Output Preview**: Format preview options
- **Spell Checking**: Dictionary selection
- **Backyard Settings**: Database configuration
- **Language Settings**: Localization options
- **Theme Settings**: Light/dark mode selection

### Dialog Elements - Complete Inventory

**AboutDialog Elements**
- Application icon
- Version information
- Copyright notice
- License information
- GitHub link
- Close button

**AssetViewDialog Elements**
- Asset grid
- Asset preview
- Metadata display
- Import button
- Export button
- Delete button
- Properties button
- Close button

**BackyardBrowserDialog Elements**
- Character list
- Search box
- Filter controls
- Import button
- Link button
- Cancel button
- Status display

**CreateRecipeDialog Elements**
- Name field
- Category dropdown
- Template editor
- Parameter grid
- Preview pane
- Save button
- Cancel button

**CreateSnippetDialog Elements**
- Name field
- Content editor
- Tags field
- Save button
- Cancel button

**EditModelSettingsDialog Elements**
- Parameter grid
- Validation indicators
- Save button
- Cancel button
- Reset button

**EnterNameDialog Elements**
- Input field
- Validation indicator
- OK button
- Cancel button

**EnterUrlDialog Elements**
- URL field
- Validation indicator
- OK button
- Cancel button

**FileFormatDialog Elements**
- Format list
- Options panel
- Export button
- Cancel button

**FindReplaceDialog Elements**
- Find field
- Replace field
- Options checkboxes
- Find button
- Replace button
- Replace all button
- Cancel button

**GenderSwapDialog Elements**
- Source text
- Target gender
- Convert button
- Cancel button

**LinkEditChatDialog Elements**
- Message list
- Message editor
- Save button
- Delete button
- Regenerate button
- Cancel button

**PasteTextDialog Elements**
- Text preview
- Options panel
- Paste button
- Cancel button

**RearrangeActorsDialog Elements**
- Actor list
- Drag-drop interface
- Add button
- Remove button
- OK button
- Cancel button

**VariablesDialog Elements**
- Variable grid
- Variable editor
- Add button
- Remove button
- Edit button
- OK button
- Cancel button

**WriteDialog Elements**
- Text editor
- Toolbar
- Format controls
- Save button
- Cancel button

### Context Menu Elements - Complete Inventory

**Character Context Menu**
- Copy Character Name
- Copy Character Data
- Paste Character Data
- Duplicate Character
- Export Character
- Import Character
- Character Properties
- Separator
- Delete Character

**Recipe Context Menu**
- Apply Recipe
- Remove Recipe
- Copy Recipe
- Paste Recipe
- Recipe Properties
- Edit Recipe
- Delete Recipe
- Separator
- Select All

**Lorebook Context Menu**
- Add Entry
- Edit Entry
- Delete Entry
- Copy Entry
- Paste Entry
- Import Entries
- Export Entries
- Entry Properties
- Separator
- Select All

**Text Context Menu**
- Cut
- Copy
- Paste
- Select All
- Find
- Replace
- Spell Check
- Format
- Separator
- Properties

### Notification Elements - Complete Inventory

**Status Bar Notifications**
- Token counter display
- Status message display
- Progress indicator
- Connection status icon
- Error indicator icon
- Warning indicator icon
- Info indicator icon

**Error Notifications**
- Error dialog
- Error message
- Error details
- Recovery options
- Close button

**Warning Notifications**
- Warning dialog
- Warning message
- Warning details
- Acknowledge button
- Close button

**Info Notifications**
- Info dialog
- Info message
- Info details
- OK button
- Close button

### Toolbar Elements - Complete Inventory

**File Operations Toolbar**
- New button
- Open button
- Save button
- Export button
- Separator
- Undo button
- Redo button

**Edit Operations Toolbar**
- Cut button
- Copy button
- Paste button
- Find button
- Replace button
- Separator
- Preferences button

**View Operations Toolbar**
- Zoom in button
- Zoom out button
- Reset zoom button
- Full screen button
- Separator
- Preview button

**Tools Toolbar**
- Recipe button
- Lorebook button
- Gender swap button
- Variables button
- Separator
- Bake all button

**Backyard Toolbar**
- Connect button
- Push button
- Pull button
- Browse button
- Separator
- Settings button

**Help Toolbar**
- Help button
- Updates button
- GitHub button
- About button

## Complete Technical Nuances

### Text Generation Nuances
- **Parameter Resolution Order**: Local → Global → Default
- **Context Inheritance**: Parent context overrides
- **Template Caching**: LRU cache with size limit
- **Error Handling**: Graceful degradation strategies
- **Performance Optimization**: Lazy evaluation
- **Memory Management**: Object pooling
- **Thread Safety**: Synchronization mechanisms

### Character Format Nuances
- **Format Detection**: File signature analysis
- **Fallback Strategies**: Multiple format attempts
- **Version Compatibility**: Backward compatibility
- **Data Migration**: Automatic format upgrades
- **Validation Rules**: Schema compliance
- **Error Recovery**: Partial data recovery
- **Performance**: Streaming vs. in-memory

### Recipe System Nuances
- **Parameter Binding**: Dynamic binding
- **Template Compilation**: JIT compilation
- **Caching Strategies**: Multi-level caching
- **Error Handling**: Template validation
- **Performance**: Batch processing
- **Memory**: Garbage collection
- **Threading**: Parallel processing

### Lorebook System Nuances
- **Key Collision**: Conflict resolution
- **Token Counting**: Real-time calculation
- **Priority System**: Weighted sorting
- **Search Optimization**: Indexed search
- **Validation Rules**: Key uniqueness
- **Error Handling**: Data integrity
- **Performance**: Lazy loading

### Backyard Integration Nuances
- **Connection Pooling**: Performance optimization
- **Transaction Management**: Atomic operations
- **Error Handling**: Retry logic
- **Data Synchronization**: Conflict resolution
- **Performance**: Batch operations
- **Security**: Access control
- **Compatibility**: Version handling

### UI System Nuances
- **Data Binding**: Two-way binding
- **Command Routing**: Event propagation
- **State Management**: Application state
- **Performance**: Virtualization
- **Accessibility**: ARIA attributes
- **Localization**: String resources
- **Theming**: Style inheritance

### Settings System Nuances
- **Persistence**: JSON serialization
- **Validation**: Schema validation
- **Fallback**: Default values
- **Migration**: Version upgrades
- **Performance**: Caching
- **Security**: Encryption
- **Compatibility**: Backward compatibility

### Error Handling Nuances
- **Error Classification**: Type hierarchy
- **Error Propagation**: Exception handling
- **Error Recovery**: Fallback mechanisms
- **Error Reporting**: Detailed messages
- **Error Logging**: Comprehensive logging
- **Error Prevention**: Input validation
- **Error Testing**: Edge case testing

### Performance Nuances
- **Caching Strategies**: Multi-level caching
- **Memory Management**: Object pooling
- **I/O Optimization**: Buffered operations
- **Parallel Processing**: Multi-threading
- **Lazy Loading**: On-demand loading
- **Batch Processing**: Bulk operations
- **Profiling**: Performance monitoring

### Security Nuances
- **Input Validation**: Sanitization
- **Output Encoding**: Prevention
- **Access Control**: Permissions
- **Data Protection**: Encryption
- **Audit Logging**: Tracking
- **Vulnerability Testing**: Scanning
- **Compliance**: Standards adherence

### Localization Nuances
- **String Resources**: External files
- **Pluralization**: Language rules
- **Gender Agreement**: Grammatical rules
- **Text Direction**: RTL/LTR
- **Font Support**: Language fonts
- **Cultural Adaptation**: Local customs
- **Fallback Mechanisms**: Default language

### Accessibility Nuances
- **Keyboard Navigation**: Full support
- **Screen Reader**: ARIA attributes
- **High Contrast**: Visual themes
- **Font Scaling**: Readability
- **Color Blindness**: Color schemes
- **Focus Management**: Navigation
- **Assistive Technologies**: Compatibility

### Extensibility Nuances
- **Plugin Architecture**: Modular design
- **API Design**: Versioned interfaces
- **Dependency Management**: Isolation
- **Error Handling**: Plugin errors
- **Performance**: Plugin optimization
- **Security**: Plugin sandboxing
- **Compatibility**: Version handling

### Documentation Nuances
- **Structure**: Modular organization
- **Formats**: Multiple formats
- **Localization**: Translated docs
- **Search**: Indexed content
- **Versioning**: Document versions
- **Accessibility**: Accessible formats
- **Maintenance**: Update process

### Quality Assurance Nuances
- **Test Coverage**: Comprehensive testing
- **Automation**: Test automation
- **Regression Testing**: Bug prevention
- **Performance Testing**: Optimization
- **Usability Testing**: User experience
- **Security Testing**: Vulnerability scanning
- **Compliance Testing**: Standards adherence

### Deployment Nuances
- **Installation**: Multiple methods
- **Updates**: Version management
- **Configuration**: Settings management
- **Dependencies**: Package management
- **Compatibility**: Platform support
- **Security**: Secure deployment
- **Monitoring**: Deployment tracking

### Support Nuances
- **Issue Tracking**: Bug management
- **Documentation**: Comprehensive guides
- **Community**: User engagement
- **Professional Support**: Technical assistance
- **Self-Help**: User resources
- **Feedback**: User input
- **Improvement**: Continuous enhancement

## Conclusion

This exhaustive technical analysis provides a complete reference of every feature, function, usage pattern, UI element, and technical nuance in the Ginger application. The document serves as both comprehensive documentation and verification of the Avalonia port's 100% feature parity with the original implementation.